using MeowOS.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MeowOS
{
    /// <summary>
    /// Логика взаимодействия для EditGroupWindow.xaml
    /// </summary>
    public partial class EditGroupWindow : Window
    {
        private GroupInfo gi;
        public GroupInfo Gi
        {
            get => gi;
            set => gi = value;
        }

        public EditGroupWindow(GroupInfo gi)
        {
            InitializeComponent();
            this.gi = gi;
            nameEdit.Text = gi.Name;
        }

        private void nameEdit_TextChanged(object sender, TextChangedEventArgs e)
        {
            UsefulThings.controlLettersAndDigits(sender as TextBox);
            okBtn.IsEnabled = nameEdit.Text.Length > 0;
        }

        private void okClick(object sender, RoutedEventArgs e)
        {
            gi.Name = nameEdit.Text;
            DialogResult = true;
        }

        private void cancelClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
