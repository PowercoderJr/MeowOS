using System.Windows;

namespace MeowOS
{
    /// <summary>
    /// Логика взаимодействия для AskPasswordWindow.xaml
    /// </summary>
    public partial class AskPasswordWindow : Window
    {
        public AskPasswordWindow()
        {
            InitializeComponent();
        }

        private void okClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void cancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
