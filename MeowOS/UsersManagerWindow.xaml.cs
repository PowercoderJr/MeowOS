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
        private List<string> groups, users;

        public UsersManagerWindow(byte[] usersData, byte[] groupsData)
        {
            InitializeComponent();
            this.usersData = usersData;
            this.groupsData = groupsData;

            groups = new List<string>(UsefulThings.fileFromByteArrToStringArr(groupsData));
            users = new List<string>(UsefulThings.fileFromByteArrToStringArr(usersData));

            string[] user = { "", "", "", "" }; //0 = login, 1 = digest, 2 = gid, 3 = role
            ushort uid, gid;
            for (uid = 1; uid <= users.Count; ++uid)
            {
                user = users[uid - 1].Split(UsefulThings.USERDATA_SEPARATOR.ToString().ToArray(), StringSplitOptions.RemoveEmptyEntries);
                gid = ushort.Parse(user[2]);
                if (user[0].First() != UsefulThings.DELETED_MARK)
                    usersListView.Items.Add(new UserInfo(uid, user[0], gid, groups[gid - 1], (UserInfo.Roles)int.Parse(user[3])));
            }
            for (int i = 1; i <= groups.Count; ++i)
                if (groups[i - 1].First() != UsefulThings.DELETED_MARK)
                    groupsListView.Items.Add(new GroupInfo(i, groups[i - 1]));
        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            (sender as Expander).Height = 140;
        }

        private void addGroupBtn_Click(object sender, RoutedEventArgs e)
        {
            GroupInfo gi = new GroupInfo(groups.Count + 1, "Новая группа");
            EditGroupWindow egw = new EditGroupWindow(gi);
            if (egw.ShowDialog().Value)
                if (groups.Contains(gi.Name))
                    MessageBox.Show("Группа с таким названием уже существует", "Ошибка");
                else
                {
                    groups.Add(gi.Name);
                    groupsListView.Items.Add(gi);
                }
        }


        private void editGroupBtn_Click(object sender, RoutedEventArgs e)
        {
            //TODO
            /*
            GroupInfo gi = groupsListView.SelectedItem as GroupInfo;
            EditGroupWindow egw = new EditGroupWindow(gi);
            if (egw.ShowDialog().Value)
            {
                var namesakes = groups.FindAll(s => s.Equals(gi.Name));
                if (namesakes.Count > 1 || namesakes.Count == 1 && )
                    MessageBox.Show("Группа с таким названием уже существует", "Ошибка");
                else
                {
                    groups.Add(gi.Name);
                    groupsListView.Items.Add(gi);
                }
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
