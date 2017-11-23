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
        private class GroupInfo
        {
            private int id;
            public int Id => id;
            private string name;
            public string Name => name;

            public GroupInfo(int id, string name)
            {
                this.id = id;
                this.name = name;
            }
        }

        private byte[] usersData, groupsData;
        private string[] groups;

        public UsersManagerWindow(byte[] usersData, byte[] groupsData)
        {
            InitializeComponent();
            this.usersData = usersData;
            this.groupsData = groupsData;

            string groupsTmp = Encoding.GetEncoding(1251).GetString(groupsData);
            groups = groupsTmp.Split(new string[] { UsefulThings.EOLN_STR }, StringSplitOptions.RemoveEmptyEntries);

            string usersTmp = Encoding.GetEncoding(1251).GetString(usersData);
            string[] users = usersTmp.Split(new string[] { UsefulThings.EOLN_STR }, StringSplitOptions.RemoveEmptyEntries);

            string[] user = { "", "", "", "" }; //0 = login, 1 = digest, 2 = gid, 3 = role
            ushort uid, gid;
            for (uid = 1; uid <= users.Length; ++uid)
            {
                user = users[uid - 1].Split(UsefulThings.USERDATA_SEPARATOR.ToString().ToArray(), StringSplitOptions.RemoveEmptyEntries);
                gid = ushort.Parse(user[2]);
                usersListView.Items.Add(new UserInfo(uid, user[0], gid, groups[gid - 1], (UserInfo.Roles)int.Parse(user[3])));
            }
            for (int i = 1; i <= groups.Length; ++i)
                groupListView.Items.Add(new GroupInfo(i, groups[i - 1]));
        }

        private void Expander_Expanded(object sender, RoutedEventArgs e)
        {
            (sender as Expander).Height = 140;
        }

        private void Expander_Collapsed(object sender, RoutedEventArgs e)
        {
            (sender as Expander).Height = 25;
        }
    }
}
