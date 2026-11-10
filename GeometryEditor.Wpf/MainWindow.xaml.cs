using GeometryEditor.Core.Editor;
using GeometryEditor.Core.Models;
using GeometryEditor.Wpf.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Point = System.Windows.Point;
using RectangleShape = System.Windows.Shapes.Rectangle;
using FigureItem = GeometryEditor.Core.Editor.FigureItem;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Brushes = System.Windows.Media.Brushes;
using Cursors = System.Windows.Input.Cursors;

namespace GeometryEditor.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

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

            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            // Перерисовка при создании или удалении фигур
            _viewModel.Figures.CollectionChanged += (sender, e) => DrawFigures();

            // Перерисовка при изменении выделения
            _viewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.SelectedFigure))
                    DrawFigures();

                if (e.PropertyName == nameof(MainViewModel.SelectedColor))
                    UpdateColorControls();
            };

            // Перерисовка после редактирования полей свойств
            _viewModel.FigureChanged += (sender, e) => DrawFigures();
            UpdateColorControls();
        }

        private void ChooseColor_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.ColorDialog
            {
                FullOpen = true,
                Color = _viewModel.SelectedColor
            };

            if (dialog.ShowDialog() !=
                System.Windows.Forms.DialogResult.OK)
                return;

            var color = dialog.Color;

            // Передаём выбранный цвет во ViewModel
            _viewModel.SelectedColor = color;
        }

        private void DrawFigures()
        {
            WorkCanvas.Children.Clear();
            _selectedShape = null;
            _resizeHandle = null;

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
                        Points = new PointCollection
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
                    System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));

                // Обводка выбранной фигуры
                bool isSelected = ReferenceEquals(_viewModel.SelectedFigure, item);
                shape.Stroke = isSelected ? Brushes.DodgerBlue : Brushes.Black;
                shape.StrokeThickness = isSelected ? 3 : 2;
                shape.Cursor = Cursors.SizeAll;

                // Положение на холсте
                Canvas.SetLeft(shape, figure.X);
                Canvas.SetTop(shape, figure.Y);
                WorkCanvas.Children.Add(shape);

                if (isSelected)
                    _selectedShape = shape;
            }

            // Добавляем маркер размеров выбранной фигуры
            if (_viewModel.SelectedFigure != null && _selectedShape != null)
                AddResizeHandle(_viewModel.SelectedFigure.Model);
        }

        // Нажатие мышкой на холст
        private void WorkCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            Point position = e.GetPosition(WorkCanvas);

            // Маркер изменения размера обрабатываем отдельно
            bool onHandle = _resizeHandle != null
                && e.OriginalSource is DependencyObject source
                && (ReferenceEquals(source, _resizeHandle)
                    || _resizeHandle.IsAncestorOf(source));

            if (onHandle && _viewModel.SelectedFigure != null)
            {
                StartDragging(_viewModel.SelectedFigure, position, true);
            }
            else
            {
                // Ищем верхнюю фигуру, в которую попал курсор
                FigureItem? item = _viewModel.FindFigure(position.X, position.Y);
                _viewModel.SelectedFigure = item;

                if (item != null)
                    StartDragging(item, position, false);
            }

            e.Handled = true;
        }

        // Начало перетаскивания или изменения размеров
        private void StartDragging(FigureItem item, Point position, bool resize)
        {
            Figure figure = item.Model;
            _draggedItem = item;
            _isResizing = resize;
            _mouseStart = position;
            _startX = figure.X;
            _startY = figure.Y;
            (_startWidth, _startHeight) = GetFigureSize(figure);

            // Мышь захватывает неподвижный Canvas, а не перемещаемый маркер
            WorkCanvas.CaptureMouse();
        }

        // Движение мышки с зажатой левой кнопкой
        private void WorkCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (_draggedItem == null)
                return;

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                StopDragging();
                return;
            }

            Point position = e.GetPosition(WorkCanvas);
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
                // Меняем координаты самой модели
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
        private void WorkCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            StopDragging();
        }

        private void WorkCanvas_LostMouseCapture(object sender, MouseEventArgs e)
        {
            _draggedItem = null;
        }

        private void StopDragging()
        {
            _draggedItem = null;
            _isResizing = false;

            if (WorkCanvas.IsMouseCaptured)
                WorkCanvas.ReleaseMouseCapture();
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
                Cursor = Cursors.SizeNWSE
            };

            Canvas.SetZIndex(_resizeHandle, 100);
            WorkCanvas.Children.Add(_resizeHandle);
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
                    polygon.Points = new PointCollection
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
            var color = _viewModel.SelectedColor;

            // Преобразуем цвет в HEX
            ColorTextBox.Text = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

            // Обновляем цвет кнопки
            ColorButton.Background = new SolidColorBrush(
                System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B));
        }
    }
}
