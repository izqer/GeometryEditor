namespace GeometryEditor.Core.Models
{
    // Круг, наследуемый от Figure
    public class Circle : Figure
    {
        private double _radius;

        public Circle(double x, double y, double radius) : base(x, y)
        {
            Radius = radius;
        }

        // Радиус круга
        public double Radius
        {
            get => _radius;
            set
            {
                // Проверяем радиус на допустимое значение
                if (!double.IsFinite(value) || value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(Radius), "Радиус должен быть положительным конечным числом.");
                _radius = value;
            }
        }

        // Проверяем попадание точки внутрь круга
        public override bool ContainsPoint(double x, double y)
        {
            double centerX = X + Radius;
            double centerY = Y + Radius;

            double dx = x - centerX;
            double dy = y - centerY;

            return dx * dx + dy * dy <= Radius * Radius;
        }
    }
}
