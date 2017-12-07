using MeowOS.Common;
using System.Windows;
using System.Windows.Controls;

namespace MeowOS
{
    /// <summary>
    /// Логика взаимодействия для EditGroupWindow.xaml
    /// </summary>
    public partial class EditGroupWindow : Window
    {
        public EditGroupWindow(GroupInfo gi)
        {
            InitializeComponent();
            nameEdit.Text = gi.Name;
            nameEdit.SelectAll();
        }

        private void nameEdit_TextChanged(object sender, TextChangedEventArgs e)
        {
            UsefulThings.controlLettersAndDigits(sender as TextBox);
            okBtn.IsEnabled = nameEdit.Text.Length > 0;
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
