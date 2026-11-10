using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using GeometryEditor.Core.Editor;
using GeometryEditor.Core.Models;
using GeometryEditor.ViewModels;
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using Point = Avalonia.Point;
using RectangleShape = Avalonia.Controls.Shapes.Rectangle;

namespace GeometryEditor.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel? _viewModel;
        private readonly Canvas _workCanvas;
        private readonly ColorPicker _figureColorPicker;
        private bool _updatingColorPicker;

        // Перетаскиваемая фигура
        private FigureItem? _draggedItem;
        private Point _mouseStart;
        private double _startX;
        private double _startY;
        private double _startWidth;
        private double _startHeight;
        private bool _isResizing;

        // Визуальный элемент выбранной фигуры и маркер размера
        private Shape? _selectedShape;
        private RectangleShape? _resizeHandle;

        public MainWindow()
        {
            InitializeComponent();

            // Получаем элементы после загрузки XAML
            _workCanvas = this.FindControl<Canvas>("WorkCanvas")
                ?? throw new InvalidOperationException("Не найден WorkCanvas в MainWindow.axaml.");
            _figureColorPicker = this.FindControl<ColorPicker>("FigureColorPicker")
                ?? throw new InvalidOperationException("Не найден FigureColorPicker в MainWindow.axaml.");

            // DataContext устанавливается при запуске приложения
            DataContextChanged += (sender, e) => AttachViewModel();

            // Передаём выбранный цвет во ViewModel
            _figureColorPicker.PropertyChanged += (sender, e) =>
            {
                if (_updatingColorPicker || _viewModel == null || e.Property.Name != "Color")
                    return;

                var color = _figureColorPicker.Color;
                _viewModel.SelectedColor = System.Drawing.Color.FromArgb(
                    color.A, color.R, color.G, color.B);
            };

            AttachViewModel();
        }

        // Подключаем общую ViewModel
        private void AttachViewModel()
        {
            if (ReferenceEquals(_viewModel, DataContext))
                return;

            if (_viewModel != null)
            {
                _viewModel.Figures.CollectionChanged -= Figures_CollectionChanged;
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
                _viewModel.FigureChanged -= ViewModel_FigureChanged;
            }

            _viewModel = DataContext as MainViewModel;
            if (_viewModel == null)
                return;

            _viewModel.Figures.CollectionChanged += Figures_CollectionChanged;
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            _viewModel.FigureChanged += ViewModel_FigureChanged;

            DrawFigures();
            UpdateColorControls();
        }

        private void Figures_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            DrawFigures();
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MainViewModel.SelectedFigure))
                DrawFigures();

            if (e.PropertyName == nameof(MainViewModel.SelectedColor))
                UpdateColorControls();
        }

        private void ViewModel_FigureChanged(object? sender, EventArgs e)
        {
            DrawFigures();
        }

        private void DrawFigures()
        {
            _workCanvas.Children.Clear();
            _selectedShape = null;
            _resizeHandle = null;

            if (_viewModel == null)
                return;

            foreach (FigureItem item in _viewModel.Figures)
            {
                Figure figure = item.Model;

                // Создаём визуальный элемент
                Shape shape = figure switch
                {
                    Circle circle => new Ellipse
                    {
                        Width = circle.Radius * 2,
                        Height = circle.Radius * 2
                    },

                    RectangleFigure rectangle => new RectangleShape
                    {
                        Width = rectangle.Width,
                        Height = rectangle.Height
                    },

                    Triangle triangle => new Polygon
                    {
                        Points = new Points
                        {
                            new Point(triangle.Width / 2, 0),
                            new Point(0, triangle.Height),
                            new Point(triangle.Width, triangle.Height)
                        }
                    },

                    _ => throw new NotSupportedException("Неизвестный тип фигуры.")
                };

                // Цвет фигуры
                var color = figure.Color;
                shape.Fill = new SolidColorBrush(
                    Avalonia.Media.Color.FromArgb(color.A, color.R, color.G, color.B));

                // Обводка выбранной фигуры
                bool isSelected = ReferenceEquals(_viewModel.SelectedFigure, item);
                shape.Stroke = isSelected ? Brushes.DodgerBlue : Brushes.Black;
                shape.StrokeThickness = isSelected ? 3 : 2;
                shape.Cursor = new Cursor(StandardCursorType.SizeAll);

                // Положение на холсте
                Canvas.SetLeft(shape, figure.X);
                Canvas.SetTop(shape, figure.Y);
                _workCanvas.Children.Add(shape);

                if (isSelected)
                    _selectedShape = shape;
            }

            // Добавляем маркер размеров выбранной фигуры
            if (_viewModel.SelectedFigure != null && _selectedShape != null)
                AddResizeHandle(_viewModel.SelectedFigure.Model);
        }

        // Нажатие мышкой на холст
        private void WorkCanvas_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (_viewModel == null || !e.GetCurrentPoint(_workCanvas).Properties.IsLeftButtonPressed)
                return;

            Point position = e.GetPosition(_workCanvas);

            // Маркер изменения размера обрабатываем отдельно
            bool onHandle = _resizeHandle != null && ReferenceEquals(e.Source, _resizeHandle);

            if (onHandle && _viewModel.SelectedFigure != null)
            {
                StartDragging(_viewModel.SelectedFigure, position, true, e.Pointer);
            }
            else
            {
                // Ищем верхнюю фигуру, в которую попал курсор
                FigureItem? item = _viewModel.FindFigure(position.X, position.Y);
                _viewModel.SelectedFigure = item;

                if (item != null)
                    StartDragging(item, position, false, e.Pointer);
            }

            e.Handled = true;
        }

        // Начало перетаскивания или изменения размеров
        private void StartDragging(FigureItem item, Point position, bool resize, IPointer pointer)
        {
            Figure figure = item.Model;
            _draggedItem = item;
            _isResizing = resize;
            _mouseStart = position;
            _startX = figure.X;
            _startY = figure.Y;
            (_startWidth, _startHeight) = GetFigureSize(figure);

            // Мышь захватывает неподвижный Canvas, а не перемещаемый маркер
            pointer.Capture(_workCanvas);
        }

        // Движение мышки с зажатой левой кнопкой
        private void WorkCanvas_PointerMoved(object? sender, PointerEventArgs e)
        {
            if (_viewModel == null || _draggedItem == null)
                return;

            if (!e.GetCurrentPoint(_workCanvas).Properties.IsLeftButtonPressed)
            {
                _draggedItem = null;
                _isResizing = false;
                e.Pointer.Capture(null);
                return;
            }

            Point position = e.GetPosition(_workCanvas);
            double dx = position.X - _mouseStart.X;
            double dy = position.Y - _mouseStart.Y;
            Figure figure = _draggedItem.Model;

            if (_isResizing)
            {
                // Для круга и квадрата сохраняем одинаковые стороны
                double delta = (dx + dy) / 2.0;

                switch (figure)
                {
                    case Circle circle:
                        circle.Radius = Math.Max(5, (_startWidth + delta) / 2.0);
                        break;

                    case RectangleFigure square when square.IsSquare:
                        double side = Math.Max(10, _startWidth + delta);
                        square.Resize(side, side);
                        break;

                    case RectangleFigure rectangle:
                        rectangle.Resize(
                            Math.Max(10, _startWidth + dx),
                            Math.Max(10, _startHeight + dy));
                        break;

                    case Triangle triangle:
                        triangle.Resize(
                            Math.Max(10, _startWidth + dx),
                            Math.Max(10, _startHeight + dy));
                        break;
                }
            }
            else
            {
                // Менчем координаты самой модели
                figure.X = _startX + dx;
                figure.Y = _startY + dy;
            }

            // Обновляем фигуру без пересоздания элементов во время захвата мыши
            UpdateSelectedShape(figure);
            UpdateResizeHandlePosition(figure);
            _viewModel.RefreshSelectedFigureProperties();
            e.Handled = true;
        }

        // Отпустили левую кнопку мыши
        private void WorkCanvas_PointerReleased(object? sender, PointerReleasedEventArgs e)
        {
            _draggedItem = null;
            _isResizing = false;
            e.Pointer.Capture(null);
        }

        private void WorkCanvas_PointerCaptureLost(object? sender, PointerCaptureLostEventArgs e)
        {
            _draggedItem = null;
            _isResizing = false;
        }

        // Создаём маркер изменения размеров
        private void AddResizeHandle(Figure figure)
        {
            _resizeHandle = new RectangleShape
            {
                Width = 12,
                Height = 12,
                Fill = Brushes.White,
                Stroke = Brushes.DodgerBlue,
                StrokeThickness = 2,
                Cursor = new Cursor(StandardCursorType.BottomRightCorner)
            };

            _resizeHandle.ZIndex = 100;
            _workCanvas.Children.Add(_resizeHandle);
            UpdateResizeHandlePosition(figure);
        }

        // Получаем размеры ограничивающего прямоугольника фигуры
        private static (double Width, double Height) GetFigureSize(Figure figure)
        {
            return figure switch
            {
                Circle circle => (circle.Radius * 2, circle.Radius * 2),
                RectangleFigure rectangle => (rectangle.Width, rectangle.Height),
                Triangle triangle => (triangle.Width, triangle.Height),
                _ => (0, 0)
            };
        }

        // Обновляем положение маркера
        private void UpdateResizeHandlePosition(Figure figure)
        {
            if (_resizeHandle == null)
                return;

            var (width, height) = GetFigureSize(figure);
            Canvas.SetLeft(_resizeHandle, figure.X + width - 6);
            Canvas.SetTop(_resizeHandle, figure.Y + height - 6);
        }

        // Обновляем размеры и положение выбранной фигуры на холсте
        private void UpdateSelectedShape(Figure figure)
        {
            if (_selectedShape == null)
                return;

            switch (figure)
            {
                case Circle circle:
                    _selectedShape.Width = circle.Radius * 2;
                    _selectedShape.Height = circle.Radius * 2;
                    break;

                case RectangleFigure rectangle:
                    _selectedShape.Width = rectangle.Width;
                    _selectedShape.Height = rectangle.Height;
                    break;

                case Triangle triangle when _selectedShape is Polygon polygon:
                    polygon.Points = new Points
                    {
                        new Point(triangle.Width / 2, 0),
                        new Point(0, triangle.Height),
                        new Point(triangle.Width, triangle.Height)
                    };
                    break;
            }

            Canvas.SetLeft(_selectedShape, figure.X);
            Canvas.SetTop(_selectedShape, figure.Y);
        }

        // Обновляем палитру по выбранному цвету
        private void UpdateColorControls()
        {
            if (_viewModel == null)
                return;

            var color = _viewModel.SelectedColor;
            _updatingColorPicker = true;
            try
            {
                _figureColorPicker.Color = Avalonia.Media.Color.FromArgb(
                    color.A, color.R, color.G, color.B);
            }
            finally
            {
                _updatingColorPicker = false;
            }
        }
    }
}
