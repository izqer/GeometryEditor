using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GeometryEditor.Core.Models;
using System.Collections.ObjectModel;

namespace GeometryEditor.Wpf.ViewModels
{
    // Главная ViewModel редактора
    public partial class MainViewModel : ObservableObject
    {
        // Коллекция созданных фигур
        public ObservableCollection<FigureItem> Figures { get; } = new();

        // Номер следующей создаваемой фигуры
        private int _nextFigureNumber = 1;

        // Выбранная фигура на холсте
        [ObservableProperty]
        private FigureItem? _selectedFigure;

        // Тип фигуры для создания
        [ObservableProperty]
        private string _selectedFigureType = "Круг";

        // Команда выбора типа фигуры
        [RelayCommand]
        private void SelectFigureType(string figureType)
        {
            SelectedFigureType = figureType;
            SelectedFigure = null;
        }

        // Координаты новой фигуры
        [ObservableProperty]
        private double _x = 100;

        [ObservableProperty]
        private double _y = 100;

        // Радиус круга
        [ObservableProperty]
        private double _radius = 50;

        // Сторона квадрата
        [ObservableProperty]
        private double _side = 100;

        // Ширина и высота прямоугольника или треугольника
        [ObservableProperty]
        private double _width = 180;

        [ObservableProperty]
        private double _height = 120;

        // Цвет создаваемой фигуры
        [ObservableProperty]
        private System.Drawing.Color _selectedColor = System.Drawing.Color.Red;

        // Сообщение в нижней строке
        [ObservableProperty]
        private string _statusMessage = "Готов к работе";

        // Создание фигуры
        [RelayCommand]
        private void CreateFigure()
        {
            try
            {
                Figure figure = SelectedFigureType switch
                {
                    "Круг" => new Circle(X, Y, Radius),

                    "Квадрат" => new RectangleFigure(
                        X, Y, Side, Side, true),

                    "Прямоугольник" => new RectangleFigure(
                        X, Y, Width, Height),

                    "Треугольник" => new Triangle(
                        X, Y, Width, Height),

                    _ => throw new ArgumentException(
                        "Неизвестный тип фигуры.")
                };

                // Цвет фигуры
                figure.Color = SelectedColor;

                // Создаём элемент списка с названием и номером
                var item = new FigureItem(
                    figure,
                    SelectedFigureType,
                    _nextFigureNumber
                );

                // Добавляем фигуру в коллекцию
                Figures.Add(item);

                // Увеличиваем номер для следующей фигуры
                _nextFigureNumber++;

                StatusMessage = $"Создана фигура: {item.DisplayName}";

            }
            catch (ArgumentException ex)
            {
                StatusMessage = $"Ошибка: {ex.Message}";
            }
        }


        // Текст кнопки зависит от выделения
        public string ActionButtonText =>
            SelectedFigure == null ? "Создать" : "Удалить";

        // Вызывается автоматически при изменении SelectedFigure
        partial void OnSelectedFigureChanged(FigureItem? value)
        {
            OnPropertyChanged(nameof(ActionButtonText));

            if (value == null)
                return;

            RefreshSelectedFigureProperties();
        }

        // Загружаем параметры выбранной фигуры в панель свойств
        public void RefreshSelectedFigureProperties()
        {
            if (SelectedFigure == null)
                return;

            Figure figure = SelectedFigure.Model;

            SelectedFigureType = SelectedFigure.Name;

            X = figure.X;
            Y = figure.Y;
            SelectedColor = figure.Color;

            switch (figure)
            {
                case Circle circle:
                    Radius = circle.Radius;
                    break;

                case RectangleFigure rectangle when rectangle.IsSquare:
                    Side = rectangle.Width;
                    break;

                case RectangleFigure rectangle:
                    Width = rectangle.Width;
                    Height = rectangle.Height;
                    break;

                case Triangle triangle:
                    Width = triangle.Width;
                    Height = triangle.Height;
                    break;
            }
        }


        // Создание или удаление фигуры
        [RelayCommand]
        private void Apply()
        {
            if (SelectedFigure == null)
            {
                CreateFigure();
            }
            else
            {
                DeleteFigure();
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

    }
}
