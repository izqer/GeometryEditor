using GeometryEditor.Core.Models;
using GeometryEditor.Wpf.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Point = System.Windows.Point;

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
        private Point _previousMousePosition;

        // Визуальный элемент выбранной фигуры и маркер размера
        private Shape? _selectedShape;
        private Thumb? _resizeThumb;

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new MainViewModel();
            DataContext = _viewModel;

            // Перерисовка при создании или удалении фигур
            _viewModel.Figures.CollectionChanged += (sender, e) =>
            {
                DrawFigures(_viewModel);
            };

            // Перерисовка при изменении выделения
            _viewModel.PropertyChanged += (sender, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.SelectedFigure))
                    DrawFigures(_viewModel);

                if (e.PropertyName == nameof(MainViewModel.SelectedColor))
                    UpdateColorControls();
            };
        }

        private void ChooseColor_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.ColorDialog
            {
                FullOpen = true,
                Color = System.Drawing.Color.Red
            };

            if (dialog.ShowDialog() !=
                System.Windows.Forms.DialogResult.OK)
                return;

            var color = dialog.Color;

            // Преобразуем цвет в HEX
            ColorTextBox.Text =
                $"#{color.R:X2}{color.G:X2}{color.B:X2}";

            // Передаём выбранный цвет во ViewModel
            if (DataContext is MainViewModel viewModel)
                viewModel.SelectedColor = color;

            // Обновляем цвет кнопки
            ColorButton.Background = new SolidColorBrush(
                System.Windows.Media.Color.FromRgb(color.R, color.G, color.B));
        }



        private void DrawFigures(MainViewModel viewModel)
        {
            WorkCanvas.Children.Clear();

            _selectedShape = null;
            _resizeThumb = null;

            foreach (FigureItem item in viewModel.Figures)
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

                    RectangleFigure rectangle => new System.Windows.Shapes.Rectangle
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

                    _ => throw new NotSupportedException(
                        "Неизвестный тип фигуры.")
                };

                // Цвет фигуры
                var color = figure.Color;

                shape.Fill = new SolidColorBrush(
                    System.Windows.Media.Color.FromArgb(
                        color.A, color.R, color.G, color.B));

                // Обводка выбранной фигуры
                bool isSelected = ReferenceEquals(
                    viewModel.SelectedFigure, item);

                shape.Stroke = isSelected
                    ? System.Windows.Media.Brushes.DodgerBlue
                    : System.Windows.Media.Brushes.Black;

                shape.StrokeThickness = isSelected ? 3 : 2;

                // Положение на холсте
                Canvas.SetLeft(shape, figure.X);
                Canvas.SetTop(shape, figure.Y);

                shape.Cursor = System.Windows.Input.Cursors.SizeAll;

                WorkCanvas.Children.Add(shape);

                if (isSelected)
                    _selectedShape = shape;
            }

            // Добавляем маркер размеров выбранной фигуры
            if (viewModel.SelectedFigure != null && _selectedShape != null)
                AddResizeThumb(viewModel.SelectedFigure.Model);
        }


        // Нажатие мышкой на холст
        private void WorkCanvas_MouseLeftButtonDown(
            object sender, MouseButtonEventArgs e)
        {
            // Маркер изменения размера обрабатывается отдельно
            if (_resizeThumb != null
                && e.OriginalSource is DependencyObject source
                && (ReferenceEquals(source, _resizeThumb)
                    || _resizeThumb.IsAncestorOf(source)))
                return;

            Point position = e.GetPosition(WorkCanvas);

            // Ищем верхнюю фигуру, в которую попал курсор
            FigureItem? item = _viewModel.Figures
                .Reverse()
                .FirstOrDefault(f =>
                    f.Model.ContainsPoint(position.X, position.Y));

            _viewModel.SelectedFigure = item;

            if (item == null)
                return;

            // Начинаем перетаскивание
            _draggedItem = item;
            _previousMousePosition = position;

            WorkCanvas.CaptureMouse();
            e.Handled = true;
        }

        // Движение мышки с зажатой левой кнопкой
        private void WorkCanvas_MouseMove(
            object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_draggedItem == null)
                return;

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                StopDragging();
                return;
            }

            Point position = e.GetPosition(WorkCanvas);

            double dx = position.X - _previousMousePosition.X;
            double dy = position.Y - _previousMousePosition.Y;

            Figure figure = _draggedItem.Model;

            // Меняем координаты самой модели
            figure.X += dx;
            figure.Y += dy;

            _previousMousePosition = position;

            // Перемещаем визуальный элемент
            if (_selectedShape != null)
            {
                Canvas.SetLeft(_selectedShape, figure.X);
                Canvas.SetTop(_selectedShape, figure.Y);
            }

            UpdateResizeThumbPosition(figure);
        }

        // Отпустили левую кнопку мыши
        private void WorkCanvas_MouseLeftButtonUp(
            object sender, MouseButtonEventArgs e)
        {
            StopDragging();
        }

        private void StopDragging()
        {
            _draggedItem = null;

            if (WorkCanvas.IsMouseCaptured)
                WorkCanvas.ReleaseMouseCapture();
        }


        // Создаём маркер изменения размеров
        private void AddResizeThumb(Figure figure)
        {
            _resizeThumb = new Thumb
            {
                Width = 12,
                Height = 12,
                Background = System.Windows.Media.Brushes.White,
                BorderBrush = System.Windows.Media.Brushes.DodgerBlue,
                BorderThickness = new Thickness(2),
                Cursor = System.Windows.Input.Cursors.SizeNWSE
            };

            _resizeThumb.DragDelta += ResizeThumb_DragDelta;

            _resizeThumb.DragCompleted += ResizeThumb_DragCompleted;

            Canvas.SetZIndex(_resizeThumb, 100);

            WorkCanvas.Children.Add(_resizeThumb);

            UpdateResizeThumbPosition(figure);
        }

        // Обновляем положение маркера
        private void UpdateResizeThumbPosition(Figure figure)
        {
            if (_resizeThumb == null)
                return;

            (double width, double height) = figure switch
            {
                Circle circle =>
                    (circle.Radius * 2, circle.Radius * 2),

                RectangleFigure rectangle =>
                    (rectangle.Width, rectangle.Height),

                Triangle triangle =>
                    (triangle.Width, triangle.Height),

                _ => (0, 0)
            };

            Canvas.SetLeft(_resizeThumb, figure.X + width - 6);
            Canvas.SetTop(_resizeThumb, figure.Y + height - 6);
        }


        // Изменение размеров за маркер
        private void ResizeThumb_DragDelta(
            object sender, DragDeltaEventArgs e)
        {
            if (_viewModel.SelectedFigure == null)
                return;

            Figure figure = _viewModel.SelectedFigure.Model;

            double dx = e.HorizontalChange;
            double dy = e.VerticalChange;

            // Изменяем размеры в самой модели
            switch (figure)
            {
                case Circle circle:
                    {
                        // Изменяем радиус только по горизонтальному смещению
                        circle.Radius = Math.Max(5, circle.Radius + dx / 2);

                        break;
                    }
                case RectangleFigure rectangle:
                    {
                        if (rectangle.IsSquare)
                        {
                            // У квадрата обе стороны меняются одинаково
                            double delta = Math.Abs(dx) >= Math.Abs(dy) ? dx : dy;
                            double side = Math.Max(10, rectangle.Width + delta);

                            rectangle.Resize(side, side);
                        }
                        else
                        {
                            rectangle.Resize(
                                Math.Max(10, rectangle.Width + dx),
                                Math.Max(10, rectangle.Height + dy));
                        }

                        break;
                    }

                case Triangle triangle:
                    {
                        triangle.Resize(
                            Math.Max(10, triangle.Width + dx),
                            Math.Max(10, triangle.Height + dy));

                        break;
                    }
            }


            // Обновляем визуальную фигуру
            UpdateSelectedShape(figure);

            // Не передвигаем маркер во время растягивания круга
            if (figure is not Circle)
                UpdateResizeThumbPosition(figure);
        }


        // Завершение изменения размера
        private void ResizeThumb_DragCompleted(
            object sender, DragCompletedEventArgs e)
        {
            if (_viewModel.SelectedFigure == null)
                return;

            // Возвращаем маркер в угол фигуры
            UpdateResizeThumbPosition(_viewModel.SelectedFigure.Model);
        }


        // Обновляем размеры выбранной фигуры на холсте
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

                case Triangle triangle
                    when _selectedShape is Polygon polygon:

                    polygon.Points = new PointCollection
            {
                new Point(triangle.Width / 2, 0),
                new Point(0, triangle.Height),
                new Point(triangle.Width, triangle.Height)
            };
                    break;
            }
        }


        // Обновляем палитру по выбранному цвету
        private void UpdateColorControls()
        {
            var color = _viewModel.SelectedColor;

            ColorTextBox.Text =
                $"#{color.R:X2}{color.G:X2}{color.B:X2}";

            ColorButton.Background = new SolidColorBrush(
                System.Windows.Media.Color.FromArgb(
                    color.A, color.R, color.G, color.B));
        }

    }
}