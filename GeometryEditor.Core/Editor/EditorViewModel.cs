using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeometryEditor.Core.Models;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Globalization;
using System.Linq;

namespace GeometryEditor.Core.Editor
{
    // Общая ViewModel для WPF и Avalonia
    public partial class EditorViewModel : ObservableObject
    {
        // Коллекция созданных фигур
        public ObservableCollection<FigureItem> Figures { get; } = new();

        // Номер следующей создаваемой фигуры
        private int _nextFigureNumber = 1;
        private bool _loadingProperties;

        // Уведомляем окно, если у фигуры изменились свойства
        public event EventHandler? FigureChanged;

        // Выбранная фигура на холсте
        [ObservableProperty]
        private FigureItem? _selectedFigure;

        // Тип фигуры для создания
        [ObservableProperty]
        private string _selectedFigureType = "Круг";

        // Координаты новой или выбранной фигуры
        [ObservableProperty]
        private string _xText = "100";

        [ObservableProperty]
        private string _yText = "100";

        // Радиус круга
        [ObservableProperty]
        private string _radiusText = "50";

        // Сторона квадрата
        [ObservableProperty]
        private string _sideText = "100";

        // Ширина и высота прямоугольника или треугольника
        [ObservableProperty]
        private string _widthText = "180";

        [ObservableProperty]
        private string _heightText = "120";

        // Цвет создаваемой или выбранной фигуры
        [ObservableProperty]
        private Color _selectedColor = Color.Red;

        // Сообщение в нижней строке
        [ObservableProperty]
        private string _statusMessage = "Готов к работе";

        // Поля свойств зависят от типа фигуры
        public bool IsCircle => SelectedFigureType == "Круг";
        public bool IsSquare => SelectedFigureType == "Квадрат";
        public bool HasWidthHeight => SelectedFigureType is "Прямоугольник" or "Треугольник";

        // Текст кнопки зависит от выделения
        public string ActionButtonText => SelectedFigure == null ? "Создать" : "Удалить";

        // Команда выбора типа фигуры
        [RelayCommand]
        private void SelectFigureType(string figureType)
        {
            SelectedFigure = null;
            SelectedFigureType = figureType;
        }

        // Обновляем поля при смене типа
        partial void OnSelectedFigureTypeChanged(string value)
        {
            OnPropertyChanged(nameof(IsCircle));
            OnPropertyChanged(nameof(IsSquare));
            OnPropertyChanged(nameof(HasWidthHeight));
        }

        // Загружаем параметры выбранной фигуры в панель свойств
        partial void OnSelectedFigureChanged(FigureItem? value)
        {
            OnPropertyChanged(nameof(ActionButtonText));

            if (value != null)
            {
                SelectedFigureType = value.Name;
                RefreshSelectedFigureProperties();
            }
        }

        // Обновляем параметры фигуры после редактирования поля
        partial void OnXTextChanged(string value) => ApplyPropertiesToSelectedFigure();
        partial void OnYTextChanged(string value) => ApplyPropertiesToSelectedFigure();
        partial void OnRadiusTextChanged(string value) => ApplyPropertiesToSelectedFigure();
        partial void OnSideTextChanged(string value) => ApplyPropertiesToSelectedFigure();
        partial void OnWidthTextChanged(string value) => ApplyPropertiesToSelectedFigure();
        partial void OnHeightTextChanged(string value) => ApplyPropertiesToSelectedFigure();
        partial void OnSelectedColorChanged(Color value) => ApplyPropertiesToSelectedFigure();

        // Загружаем параметры модели после выделения или перемещения мышью
        public void RefreshSelectedFigureProperties()
        {
            if (SelectedFigure == null)
                return;

            Figure figure = SelectedFigure.Model;

            _loadingProperties = true;
            try
            {
                SelectedFigureType = SelectedFigure.Name;
                XText = FormatNumber(figure.X);
                YText = FormatNumber(figure.Y);
                SelectedColor = figure.Color;

                switch (figure)
                {
                    case Circle circle:
                        RadiusText = FormatNumber(circle.Radius);
                        break;

                    case RectangleFigure square when square.IsSquare:
                        SideText = FormatNumber(square.Width);
                        break;

                    case RectangleFigure rectangle:
                        WidthText = FormatNumber(rectangle.Width);
                        HeightText = FormatNumber(rectangle.Height);
                        break;

                    case Triangle triangle:
                        WidthText = FormatNumber(triangle.Width);
                        HeightText = FormatNumber(triangle.Height);
                        break;
                }
            }
            finally
            {
                _loadingProperties = false;
            }
        }

        // Ищем последнюю нарисованную фигуру под курсором
        public FigureItem? FindFigure(double x, double y)
        {
            return Figures.Reverse()
                .FirstOrDefault(item => item.Model.ContainsPoint(x, y));
        }

        // Создание или удаление фигуры
        [RelayCommand]
        private void Apply()
        {
            if (SelectedFigure == null)
                CreateFigure();
            else
                DeleteFigure();
        }

        // Создание фигуры
        private void CreateFigure()
        {
            if (!TryReadProperties(out double x, out double y, out double size1, out double size2))
                return;

            try
            {
                Figure figure = SelectedFigureType switch
                {
                    "Круг" => new Circle(x, y, size1),
                    "Квадрат" => new RectangleFigure(x, y, size1, size1, true),
                    "Прямоугольник" => new RectangleFigure(x, y, size1, size2),
                    "Треугольник" => new Triangle(x, y, size1, size2),
                    _ => throw new ArgumentException("Неизвестный тип фигуры.")
                };

                // Цвет фигуры
                figure.Color = SelectedColor;

                // Создаём элемент списка с названием и номером
                var item = new FigureItem(figure, SelectedFigureType, _nextFigureNumber);

                // Добавляем фигуру в коллекцию
                Figures.Add(item);
                _nextFigureNumber++;
                StatusMessage = $"Создана фигура: {item.DisplayName}";
            }
            catch (ArgumentException ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
            }
        }

        // Удаление выбранной фигуры
        private void DeleteFigure()
        {
            if (SelectedFigure == null)
                return;

            FigureItem item = SelectedFigure;
            SelectedFigure = null;
            Figures.Remove(item);
            StatusMessage = $"Удалена фигура: {item.DisplayName}";
        }

        // Применяем изменения полей к выбранной фигуре
        private void ApplyPropertiesToSelectedFigure()
        {
            if (_loadingProperties || SelectedFigure == null)
                return;

            if (!TryReadProperties(out double x, out double y, out double size1, out double size2))
                return;

            try
            {
                Figure figure = SelectedFigure.Model;

                // Сначала проверяем размеры, потом сохраняем координаты
                switch (figure)
                {
                    case Circle circle:
                        if (size1 <= 0)
                            throw new ArgumentOutOfRangeException(nameof(size1), "Радиус должен быть больше нуля.");
                        circle.Radius = size1;
                        break;

                    case RectangleFigure square when square.IsSquare:
                        square.Resize(size1, size1);
                        break;

                    case RectangleFigure rectangle:
                        rectangle.Resize(size1, size2);
                        break;

                    case Triangle triangle:
                        triangle.Resize(size1, size2);
                        break;
                }

                figure.X = x;
                figure.Y = y;
                figure.Color = SelectedColor;

                StatusMessage = $"Изменена фигура: {SelectedFigure.DisplayName}";
                FigureChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (ArgumentException ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
            }
        }

        // Проверяем ввод и получаем размеры нужной фигуры
        private bool TryReadProperties(out double x, out double y, out double size1, out double size2)
        {
            x = y = size1 = size2 = 0;

            if (!TryReadNumber(XText, "X", out x) || !TryReadNumber(YText, "Y", out y))
                return false;

            return SelectedFigureType switch
            {
                "Круг" => TryReadNumber(RadiusText, "Радиус", out size1),
                "Квадрат" => TryReadNumber(SideText, "Сторона", out size1),
                "Прямоугольник" or "Треугольник" =>
                    TryReadNumber(WidthText, "Ширина", out size1)
                    && TryReadNumber(HeightText, "Высота", out size2),
                _ => SetUnknownTypeError()
            };
        }

        private bool SetUnknownTypeError()
        {
            StatusMessage = "Ошибка: неизвестный тип фигуры.";
            return false;
        }

        // Числа можно вводить с точкой или запятой
        private bool TryReadNumber(string text, string name, out double number)
        {
            string normalized = (text ?? "").Trim().Replace(',', '.');

            if (double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out number)
                && double.IsFinite(number))
                return true;

            StatusMessage = $"Ошибка: поле «{name}» должно содержать конечное число.";
            return false;
        }

        private static string FormatNumber(double number)
        {
            return number.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
