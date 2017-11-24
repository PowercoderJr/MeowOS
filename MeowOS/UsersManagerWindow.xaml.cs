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
    /// Логика взаимодействия для UsersManagerWindow.xaml
    /// </summary>
    public partial class UsersManagerWindow : Window
    {
        private byte[] usersData, groupsData;
        public byte[] UsersData => usersData;
        public byte[] GroupsData => groupsData;
        private List<GroupInfo> groups;
        private List<UserInfo> users;

        public UsersManagerWindow(byte[] usersData, byte[] groupsData)
        {
            InitializeComponent();
            this.usersData = usersData;
            this.groupsData = groupsData;

            string[] tmp = UsefulThings.fileFromByteArrToStringArr(groupsData);
            groups = new List<GroupInfo>();
            for (int i = 1; i <= tmp.Length; ++i)
            {
                GroupInfo gi = new GroupInfo(i, tmp[i - 1]);
                groups.Add(gi);
                if (gi.Name[0] != UsefulThings.DELETED_MARK)
                    groupsListView.Items.Add(gi);
            }
            
            tmp = UsefulThings.fileFromByteArrToStringArr(usersData);
            users = new List<UserInfo>();
            for (ushort i = 1; i <= tmp.Length; ++i)
            {
                string[] tokens = tmp[i - 1].Split(UsefulThings.USERDATA_SEPARATOR.ToString().ToArray(), StringSplitOptions.RemoveEmptyEntries);
                ushort gid = ushort.Parse(tokens[2]);
                UserInfo ui = new UserInfo(i, tokens[0], tokens[1], gid, groups[gid - 1].ToString(), (UserInfo.Roles)Enum.Parse(typeof(UserInfo.Roles), tokens[3]));
                users.Add(ui);
                if (ui.Login[0] != UsefulThings.DELETED_MARK)
                    usersListView.Items.Add(ui);
            }
        }

        private void reloadGroups()
        {
            groupsListView.Items.Clear();
            foreach (GroupInfo curr in groups)
                if (curr.Name[0] != UsefulThings.DELETED_MARK)
                    groupsListView.Items.Add(curr);
        }

        private void reloadUsers()
        {
            usersListView.Items.Clear();
            foreach (UserInfo curr in users)
                if (curr.Login[0] != UsefulThings.DELETED_MARK)
                    usersListView.Items.Add(curr);
        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            (sender as Expander).Height = 140;
        }

        private void addGroupBtn_Click(object sender, RoutedEventArgs e)
        {
            GroupInfo gi = new GroupInfo(groups.Count + 1, "Новаягруппа");
            EditGroupWindow egw = new EditGroupWindow(gi);
            if (egw.ShowDialog().Value)
            {
                string newName = egw.nameEdit.Text;
                if (groups.Find(item => item.Name.Equals(newName)) != null)
                    MessageBox.Show("Группа с таким названием уже существует", "Ошибка");
                else
                {
                    gi.Name = newName;
                    groups.Add(gi);
                    groupsListView.Items.Add(gi);
                }
            }
        }

        private void editGroupBtn_Click(object sender, RoutedEventArgs e)
        {
            GroupInfo gi = groupsListView.SelectedItem as GroupInfo;
            EditGroupWindow egw = new EditGroupWindow(gi);
            if (egw.ShowDialog().Value)
            {
                string newName = egw.nameEdit.Text;
                List<GroupInfo> namesakes = groups.FindAll(item => item.Name.Equals(newName));
                if (namesakes.Count > 1 || namesakes.Count == 1 && namesakes[0].Id != (groupsListView.SelectedItem as GroupInfo).Id)
                    MessageBox.Show("Группа с таким названием уже существует", "Ошибка");
                else
                {
                    int index = groupsListView.SelectedIndex;
                    gi.Name = newName;
                    reloadGroups();
                    groupsListView.SelectedIndex = index;

                    foreach (UserInfo ui in users)
                        if (ui.Gid == gi.Id)
                            ui.Group = gi.Name;
                    reloadUsers();
                }
            }
        }

        private void deleteGroupBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Вы действительно хотите удалить группу \"" + (groupsListView.SelectedItem as GroupInfo) + "\"?", "Подтвердите действие", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                int index = groupsListView.SelectedIndex;
                int gid = (groupsListView.SelectedItem as GroupInfo).Id;
                groupsListView.Items.RemoveAt(index);
                groups[gid - 1].Name = UsefulThings.DELETED_MARK + groups[gid - 1].Name.Remove(0, 1);
            }
        }

        private void addUserBtn_Click(object sender, RoutedEventArgs e)
        {
            UserInfo ui = new UserInfo((ushort)users.Count, "Новыйпользователь", 1, groups[0].Name, UserInfo.Roles.USER);
            EditUserWindow euw = new EditUserWindow(ui, groups.FindAll(item => item.Name[0] != UsefulThings.DELETED_MARK));
            if (euw.ShowDialog().Value)
                ;
            /*GroupInfo gi = new GroupInfo(groups.Count + 1, "Новая группа");
            EditGroupWindow egw = new EditGroupWindow(gi);
            if (egw.ShowDialog().Value)
            {
                string newName = egw.nameEdit.Text;
                if (groups.Find(item => item.Name.Equals(newName)) != null)
                    MessageBox.Show("Группа с таким названием уже существует", "Ошибка");
                else
                {
                    gi.Name = newName;
                    groups.Add(gi);
                    groupsListView.Items.Add(gi);
                }
            }*/
        }

        private void editUserBtn_Click(object sender, RoutedEventArgs e)
        {
            /*GroupInfo gi = groupsListView.SelectedItem as GroupInfo;
            EditGroupWindow egw = new EditGroupWindow(gi);
            if (egw.ShowDialog().Value)
            {
                string newName = egw.nameEdit.Text;
                List<GroupInfo> namesakes = groups.FindAll(item => item.Name.Equals(newName));
                if (namesakes.Count > 1 || namesakes.Count == 1 && namesakes[0].Id != (groupsListView.SelectedItem as GroupInfo).Id)
                    MessageBox.Show("Группа с таким названием уже существует", "Ошибка");
                else
                {
                    int index = groupsListView.SelectedIndex;
                    gi.Name = newName;
                    reloadGroups();
                    groupsListView.SelectedIndex = index;

                    foreach (UserInfo ui in users)
                        if (ui.Gid == gi.Id)
                            ui.Group = gi.Name;
                    reloadUsers();
                }
            }*/
        }

        private void deleteUserBtn_Click(object sender, RoutedEventArgs e)
        {
            /*if (MessageBox.Show("Вы действительно хотите удалить группу \"" + (groupsListView.SelectedItem as GroupInfo) + "\"?", "Подтвердите действие", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                int index = groupsListView.SelectedIndex;
                int gid = (groupsListView.SelectedItem as GroupInfo).Id;
                groupsListView.Items.RemoveAt(index);
                groups[gid - 1].Name = UsefulThings.DELETED_MARK + groups[gid - 1].Name.Remove(0, 1);
            }*/
        }

        private void Expander_Collapsed(object sender, RoutedEventArgs e)
        {
            (sender as Expander).Height = 25;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            usersData = UsefulThings.ENCODING.GetBytes(string.Join(UsefulThings.EOLN_STR, users));
            groupsData = UsefulThings.ENCODING.GetBytes(string.Join(UsefulThings.EOLN_STR, groups));
        }
    }
}
