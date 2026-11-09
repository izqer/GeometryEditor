using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GeometryEditor.Wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
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

            // Обновляем цвет кнопки
            ColorButton.Background = new SolidColorBrush(
                System.Windows.Media.Color.FromRgb(color.R, color.G, color.B));
        }
    }
}