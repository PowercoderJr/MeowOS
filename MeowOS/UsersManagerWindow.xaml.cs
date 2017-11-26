using MeowOS.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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
            for (ushort i = 1; i <= tmp.Length; ++i)
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

            addUserBtn.IsEnabled = addGroupBtn.IsEnabled = Session.userInfo.Role == UserInfo.Roles.ADMIN;
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

        private void Expander_Collapsed(object sender, RoutedEventArgs e)
        {
            (sender as Expander).Height = 25;
        }
        
        private void addGroupBtn_Click(object sender, RoutedEventArgs e)
        {
            GroupInfo gi = new GroupInfo((ushort)(groups.Count + 1), "Новаягруппа");
            EditGroupWindow egw = new EditGroupWindow(gi);
            if (egw.ShowDialog().Value)
            {
                string newName = egw.nameEdit.Text;
                if (groups.Find(item => item.Name.Equals(newName)) != null)
                    MessageBox.Show("Группа с таким названием уже существует", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                    MessageBox.Show("Группа с таким названием уже существует", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
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

                    if (Session.userInfo.Gid == gi.Id )
                        Session.userInfo.Group = gi.Name;
                }
            }
        }

        private void deleteGroupBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Вы действительно хотите удалить группу \"" + (groupsListView.SelectedItem as GroupInfo).Name + 
                "\"?\r\nЕсли в системе есть пользователи, принадлежащие этой группе, они будут перемещены в группу \"" +
                groups[0].Name + "\" (id=1). При этом все файлы на диске сохранят прежнее значение GID владельца.", "Подтвердите действие",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                int index = groupsListView.SelectedIndex;
                ushort gid = (groupsListView.SelectedItem as GroupInfo).Id;
                groupsListView.Items.RemoveAt(index);
                groups[gid - 1].Name = UsefulThings.DELETED_MARK + groups[gid - 1].Name.Remove(0, 1);
                List<UserInfo> immigrants = users.FindAll(item => item.Gid == gid);
                if (immigrants.Count > 0)
                {
                    foreach (UserInfo ui in immigrants)
                    {
                        ui.Gid = 1;
                        ui.Group = groups[0].Name;
                    }
                    reloadUsers();
                }
                if (Session.userInfo.Gid == gid)
                {
                    Session.userInfo.Gid = 1;
                    Session.userInfo.Group = groups[0].Name;
                }
            }
        }

        private void addUserBtn_Click(object sender, RoutedEventArgs e)
        {
            UserInfo ui = new UserInfo((ushort)(users.Count + 1), "Новыйпользователь", 1, groups[0].Name, UserInfo.Roles.USER);
            EditUserWindow euw = new EditUserWindow(ui, groups.FindAll(item => item.Name[0] != UsefulThings.DELETED_MARK));
            euw.changePassChb.IsChecked = true;
            euw.changePassChb.IsEnabled = false;
            if (euw.ShowDialog().Value)
            {
                string newLogin = euw.loginEdit.Text;
                if (users.Find(item => item.Login.Equals(newLogin)) != null)
                    MessageBox.Show("Пользователь с таким логином уже существует", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                else
                {
                    SHA1 sha = SHA1.Create();
                    ui.Login = newLogin;
                    ui.Digest = UsefulThings.ENCODING.GetString(sha.ComputeHash(UsefulThings.ENCODING.GetBytes(euw.pass1Edit.Password)));
                    ui.Digest = UsefulThings.replaceControlChars(ui.Digest);
                    ui.Gid = (euw.groupCB.SelectedItem as GroupInfo).Id;
                    ui.Group = (euw.groupCB.SelectedItem as GroupInfo).Name;
                    ui.Role = (UserInfo.Roles)Enum.Parse(typeof(UserInfo.Roles), euw.roleCB.SelectedItem.ToString());
                    users.Add(ui);
                    usersListView.Items.Add(ui);
                }
            }
        }

        private void editUserBtn_Click(object sender, RoutedEventArgs e)
        {
            UserInfo ui = usersListView.SelectedItem as UserInfo;
            EditUserWindow euw = new EditUserWindow(ui, groups.FindAll(item => item.Name[0] != UsefulThings.DELETED_MARK));
            if (ui.Uid == 1 || Session.userInfo.Role == UserInfo.Roles.USER)
            {
                euw.groupCB.IsEnabled = false;
                euw.roleCB.IsEnabled = false;
            }
            if (euw.ShowDialog().Value)
            {
                string newLogin = euw.loginEdit.Text;
                List<UserInfo> namesakes = users.FindAll(item => item.Login.Equals(newLogin));
                if (namesakes.Count > 1 || namesakes.Count == 1 && namesakes[0].Uid != (usersListView.SelectedItem as UserInfo).Uid)
                    MessageBox.Show("Пользователь с таким логином уже существует", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                else
                {
                    //TODO 26.11: запросить старый пароль, если изменения внёс USER
                    int index = usersListView.SelectedIndex;
                    SHA1 sha = SHA1.Create();
                    ui.Login = newLogin;
                    ui.Digest = UsefulThings.ENCODING.GetString(sha.ComputeHash(UsefulThings.ENCODING.GetBytes(euw.pass1Edit.Password)));
                    ui.Digest = UsefulThings.replaceControlChars(ui.Digest);
                    ui.Gid = (euw.groupCB.SelectedItem as GroupInfo).Id;
                    ui.Group = (euw.groupCB.SelectedItem as GroupInfo).Name;
                    ui.Role = (UserInfo.Roles)Enum.Parse(typeof(UserInfo.Roles), euw.roleCB.SelectedItem.ToString());
                    reloadUsers();
                    usersListView.SelectedIndex = index;

                    if (Session.userInfo.Uid == ui.Uid)
                        Session.userInfo = new UserInfo(ui);
                }
            }
        }

        private void deleteUserBtn_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Вы действительно хотите удалить пользователя \"" + (usersListView.SelectedItem as UserInfo).Login +
                "\"?", "Подтвердите действие", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                int index = usersListView.SelectedIndex;
                int uid = (usersListView.SelectedItem as UserInfo).Uid;
                usersListView.Items.RemoveAt(index);
                users[uid - 1].Login = UsefulThings.DELETED_MARK + users[uid - 1].Login.Remove(0, 1);
            }
        }

        private void usersListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UserInfo item = usersListView.SelectedItem as UserInfo;
            if (Session.userInfo.Role == UserInfo.Roles.ADMIN)
            {
                editUserBtn.IsEnabled = deleteUserBtn.IsEnabled = item != null;
                if (item != null && (item.Uid == 1 || item.Uid == Session.userInfo.Uid))
                    deleteUserBtn.IsEnabled = false;
            }
            else
            {
                editUserBtn.IsEnabled = item != null && item.Uid == Session.userInfo.Uid;
            }
        }

        private void groupsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Session.userInfo.Role == UserInfo.Roles.ADMIN)
            {
                GroupInfo item = groupsListView.SelectedItem as GroupInfo;
                editGroupBtn.IsEnabled = deleteGroupBtn.IsEnabled = item != null;
                if (item != null && item.Id == 1)
                    deleteGroupBtn.IsEnabled = false;
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            usersData = UsefulThings.ENCODING.GetBytes(string.Join(UsefulThings.EOLN_STR, users));
            groupsData = UsefulThings.ENCODING.GetBytes(string.Join(UsefulThings.EOLN_STR, groups));
        }
    }
}
