namespace GeometryEditor.Core.Models
{
    // Прямоугольник или квадрат, наследуемый от Figure
    public class RectangleFigure : Figure
    {
        // Размеры фигуры
        public double Width { get; private set; }
        public double Height { get; private set; }

        // Является ли фигура квадратом?
        public bool IsSquare { get; }

        public RectangleFigure(
            double x,
            double y,
            double width,
            double height,
            bool isSquare = false) : base(x, y)
        {
            IsSquare = isSquare;
            Resize(width, height);
        }

        // Изменение размеров фигуры
        public void Resize(double width, double height)
        {
            // Проверяем ширину
            if (!double.IsFinite(width) || width <= 0)
                throw new ArgumentOutOfRangeException(nameof(width), "Ширина должна быть положительным конечным числом.");

            // Проверяем высоту
            if (!double.IsFinite(height) || height <= 0)
                throw new ArgumentOutOfRangeException(nameof(height), "Высота должна быть положительным конечным числом.");

            // Для квадрата стороны должны быть равны
            if (IsSquare && width != height)
                throw new ArgumentException("У квадрата ширина и высота должны быть равны.");

            Width = width;
            Height = height;
        }

        // Проверяем, находится ли точка внутри фигуры
        public override bool ContainsPoint(double x, double y)
        {
            return x >= X && x <= X + Width
                && y >= Y && y <= Y + Height;
        }
    }
}
