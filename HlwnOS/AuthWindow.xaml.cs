using HlwnOS.FileSystem;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace HlwnOS
{
    /// <summary>
    /// Логика взаимодействия для AuthWindow.xaml
    /// </summary>
    public partial class AuthWindow : Window
    {
        public AuthWindow()
        {
            InitializeComponent();
        }

        //TODO 18.11: разобраться с повторным кодом open и create
        //            обработать исключения
        //            игнорировать регистр логина
        private void openClick(object sender, RoutedEventArgs e)
        {
            statusLabel.Visibility = Visibility.Hidden;
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.DefaultExt = "hfs";
            dialog.Filter = "Hlwn disk (*.hfs)|*.hfs";
            if (dialog.ShowDialog() == true)
            {
                Controller ctrl = new Controller();
                try
                {
                    ctrl.openSpace(dialog.FileName);
                    byte[] users = ctrl.readFile("/", "users", "sys");
                    bool accessed = false;
                    string[] user = { "", "", "", "" }; //0 = login, 1 = digest, 2 = gid, 3 = role
                    SHA1 sha = SHA1.Create();
                    int uid = 0;
                    while (!accessed && users.Length > 0)
                    {
                        ++uid;
                        user = Encoding.ASCII.GetString(users.Take(Array.IndexOf(users, UsefulThings.EOLN_BYTES[0])).ToArray()).Split(UsefulThings.USERDATA_SEPARATOR);
                        accessed = user[0].Equals(loginEdit.Text) && user[1].Equals(Encoding.ASCII.GetString(sha.ComputeHash(Encoding.ASCII.GetBytes(passEdit.Password))));
                        users = users.Skip(Array.IndexOf(users, UsefulThings.EOLN_BYTES.Last()) + 1).ToArray();
                    }

                    if (accessed)
                    {
                        byte[] groups = ctrl.readFile("/", "groups", "sys");
                        int gid = int.Parse(user[2]);
                        string group = "";
                        for (int i = 1; i < gid && groups.Length > 0; ++i)
                            groups = groups.Skip(Array.IndexOf(groups, UsefulThings.EOLN_BYTES.Last())).ToArray();
                        UserInfo userInfo = new UserInfo(uid, user[0], int.Parse(user[2]), group, (UserInfo.Roles)int.Parse(user[3]));
                        Session.userInfo = userInfo;
                        MainWindow mw = new MainWindow(dialog.FileName);
                        Close();
                        mw.Show();
                    }
                    else
                    {
                        statusLabel.Content = "Доступ не разрешён";
                        statusLabel.Visibility = Visibility.Visible;
                    }
                }
                catch
                {
                    ctrl.closeSpace();
                }
            }
        }

        private void createClick(object sender, RoutedEventArgs e)
        {
            //TODO 15.11: запрашивать параметры создаваемого диска?
            statusLabel.Visibility = Visibility.Hidden;
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.DefaultExt = "hfs";
            dialog.Filter = "Hlwn disk (*.hfs)|*.hfs";
            if (dialog.ShowDialog() == true)
            {
                Controller ctrl = new Controller();
                ushort clusterSize = Controller.FACTOR * 4; //Блок = 4 КБ
                ushort rootSize = (ushort)(clusterSize * 10); //Корневой каталог = 10 блоков
                uint diskSize = 1 * Controller.FACTOR * Controller.FACTOR; //Раздел = 50 МБ (или 1 МБ для тестов)
                ctrl.SuperBlock = new SuperBlock(ctrl, "HlwnFS", clusterSize, rootSize, diskSize);
                ctrl.Fat = new FAT(ctrl, (int)(diskSize / clusterSize));
                ctrl.RootDir = Encoding.ASCII.GetBytes(new String('\0', rootSize));

                SHA1 sha = SHA1.Create();
                bool success = true;
                try
                {
                    ctrl.createSpace(dialog.FileName, loginEdit.Text, Encoding.ASCII.GetString(sha.ComputeHash(Encoding.ASCII.GetBytes(passEdit.Password))));
                }
                catch
                {
                    success = false;
                }
                finally
                {
                    ctrl.closeSpace();
                }

                if (success)
                {
                    UserInfo userInfo = new UserInfo(1, loginEdit.Text, 1, UserInfo.DEFAULT_GROUP, UserInfo.Roles.ADMIN);
                    Session.userInfo = userInfo;
                    MainWindow mw = new MainWindow(dialog.FileName);
                    Close();
                    mw.Show();
                }
            }
        }

        private void loginEdit_TextChanged(object sender, TextChangedEventArgs e)
        {
            UsefulThings.controlLettersAndDigits(sender as TextBox);
        }
    }
}
