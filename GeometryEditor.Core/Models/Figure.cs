using System.Drawing;

namespace GeometryEditor.Core.Models
{
    // Абстрактный класс для всех фигур. Сам по себе не создаётся, используется как основа для наследников для других классов от этого него по типу круга, треугольника, квадрата, прямоугольник и т.п.
    public abstract class Figure
    {
        private double _x;
        private double _y;

        protected Figure(double x, double y)
        {
            X = x;
            Y = y;
        }

        // ---
        // Инкапсулируем X и Y
        public double X
        {
            get => _x; // Возвращаем X
            set
            {
                // Проверка на NaN или бесконечность
                if (!double.IsFinite(value)) 
                    throw new ArgumentOutOfRangeException(nameof(X), "Координата X должна быть конечным числом."); // Вызываем исключение
                _x = value; // Сохраняем X
            }
        }

        public double Y
        {
            get => _y;
            set
            {
                if (!double.IsFinite(value))
                    throw new ArgumentOutOfRangeException(nameof(Y), "Координата Y должна быть конечным числом.");
                _y = value;
            }
        }
        // ---

        public Color Color { get; set; } = System.Drawing.Color.White; // Цвет фигуры

        /// <summary>
        /// Проверка принадлежности точки фигуре. Полиморфизм.
        /// </summary>
        /// <param name="x">Координата X точки</param>
        /// <param name="y">Координата Y точки</param>
        /// <returns>True, если точка принадлежит фигуре</returns>
        public abstract bool ContainsPoint(double x, double y);
    }
}
