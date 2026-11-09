namespace GeometryEditor.Core.Models
{
    // Равнобедренный треугольник, наследуемый от Figure
    public class Triangle : Figure
    {
        // Размеры фигуры
        public double Width { get; private set; }
        public double Height { get; private set; }

        public Triangle(
            double x,
            double y,
            double width,
            double height) : base(x, y)
        {
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
            Width = width;
            Height = height;
        }

        // Проверяем, находится ли точка внутри фигуры
        public override bool ContainsPoint(double x, double y)
        {
            // Относительные координаты точки
            double relativeX = (x - X) / Width;
            double relativeY = (y - Y) / Height;
            // Проверяем попадание внутрь треугольника
            return relativeY >= 0 && relativeY <= 1
                && Math.Abs(relativeX - 0.5) <= relativeY / 2;
        }
    }
}
