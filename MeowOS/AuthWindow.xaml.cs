using MeowOS.FileSystem;
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

namespace MeowOS
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
        
        private void authorize(string login, string password, bool createNew, ContentControl statusControl)
        {
            statusControl.Visibility = Visibility.Hidden;
            FileDialog dialog = createNew ? (FileDialog)new SaveFileDialog() : (FileDialog)new OpenFileDialog();
            dialog.DefaultExt = "mfs";
            dialog.Filter = "Meow disk (*.mfs)|*.mfs";
            UserInfo userInfo = null;
            if (dialog.ShowDialog() == true)
            {
                FileSystemController fsctrl = new FileSystemController();
                SHA1 sha = SHA1.Create();
                string digest = Encoding.GetEncoding(1251).GetString(sha.ComputeHash(Encoding.GetEncoding(1251).GetBytes(password)));
                //Замена управляющих символов
                digest = digest.Replace('|', 'x');
                digest = digest.Replace('\r', 's');
                digest = digest.Replace('\n', 'f');
                bool success;

                try
                {
                    if (createNew)
                    {
                        //Создать
                        //TODO 15.11: запрашивать параметры создаваемого диска?
                        ushort clusterSize = FileSystemController.FACTOR * 4; //Блок = 4 КБ
                        ushort rootSize = (ushort)(clusterSize * 10); //Корневой каталог = 10 блоков
                        uint diskSize = 1 * FileSystemController.FACTOR * FileSystemController.FACTOR; //Раздел = 50 МБ (или 1 МБ для тестов)
                        fsctrl.SuperBlock = new SuperBlock(fsctrl, "MeowFS", clusterSize, rootSize, diskSize);
                        fsctrl.Fat = new FAT(fsctrl, (int)(diskSize / clusterSize));
                        fsctrl.RootDir = Encoding.GetEncoding(1251).GetBytes(new String('\0', rootSize));
                        fsctrl.createSpace(dialog.FileName, login, digest);
                        userInfo = new UserInfo(1, login, 1, UserInfo.DEFAULT_GROUP, UserInfo.Roles.ADMIN);
                        success = true;
                    }
                    else
                    {
                        //Открыть
                        //TODO 24.11: вместо UsefulThings.readLine и UsefulThings.skipLine использовать способ из UserManagerWindow? (Encoding... Split...)
                        fsctrl.openSpace(dialog.FileName);
                        byte[] users = fsctrl.readFile("/users.sys");
                        string[] user = { "","","","" }; //0 = login, 1 = digest, 2 = gid, 3 = role
                        int uid = 0;
                        success = false;
                        while (!success && users.Length > 0)
                        {
                            ++uid;
                            user = UsefulThings.readLine(users).Split(UsefulThings.USERDATA_SEPARATOR.ToString().ToArray(), StringSplitOptions.None);

                            success = user[0].ToLower().Equals(login.ToLower()) && user[1].Equals(digest);
                            users = UsefulThings.skipLine(users);
                        }
                        if (success)
                        {
                            byte[] groups = fsctrl.readFile("/groups.sys");
                            int gid = int.Parse(user[2]);
                            for (int i = 1; i < gid && groups.Length > 0; ++i)
                                groups = UsefulThings.skipLine(groups);

                            //Может ли быть пользователь без группы?
                            if (groups.Length > 0)
                                userInfo = new UserInfo((ushort)uid, user[0], (ushort)int.Parse(user[2]), UsefulThings.readLine(groups), (UserInfo.Roles)int.Parse(user[3]));
                            else
                                success = false;
                        }
                    }
                }
                catch
                {
                    success = false;
                }
                finally
                {
                    fsctrl.closeSpace();
                }

                if (success)
                {
                    Session.userInfo = userInfo;
                    MainWindow mw = new MainWindow(dialog.FileName, userInfo.Role);
                    Close();
                    mw.Show();
                }
                else
                {
                    statusControl.Content = "Доступ не разрешён";
                    statusControl.Visibility = Visibility.Visible;
                }
            }
        }
        
        private void openClick(object sender, RoutedEventArgs e)
        {
            authorize(loginEdit.Text, passEdit.Password, false, statusLabel);
        }

        private void createClick(object sender, RoutedEventArgs e)
        {
            authorize(loginEdit.Text, passEdit.Password, true, statusLabel);
        }

        private void loginEdit_TextChanged(object sender, TextChangedEventArgs e)
        {
            UsefulThings.controlLettersAndDigits(sender as TextBox);
            openBtn.IsEnabled = createBtn.IsEnabled = loginEdit.Text.Length > 0;
        }
    }
}
