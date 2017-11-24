using MeowOS.Common;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace MeowOS
{
    /// <summary>
    /// Логика взаимодействия для EditUserWindow.xaml
    /// </summary>
    public partial class EditUserWindow : Window
    {
        public EditUserWindow(UserInfo ui, List<GroupInfo> groups)
        {
            InitializeComponent();
            groupCB.ItemsSource = groups;
            roleCB.ItemsSource = Enum.GetNames(typeof(UserInfo.Roles));
            loginEdit.Text = ui.Login;
            groupCB.SelectedIndex = groups.FindIndex(item => item.Name.Equals(ui.Group));
            roleCB.SelectedIndex = (int)(ui.Role);
        }

        private void changePassChbChanged(object sender, RoutedEventArgs e)
        {
            changePassGrid.Visibility = (sender as CheckBox).IsChecked.Value ? Visibility.Visible : Visibility.Hidden;
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
