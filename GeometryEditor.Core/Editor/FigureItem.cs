using GeometryEditor.Core.Models;

namespace GeometryEditor.Core.Editor
{
    // Элемент списка созданных фигур
    public class FigureItem
    {
        public Figure Model { get; }
        public string Name { get; }
        public int Number { get; }

        // Название для списка
        public string DisplayName => $"{Name} №{Number}";

        public FigureItem(Figure model, string name, int number)
        {
            Model = model;
            Name = name;
            Number = number;
        }
    }
}
