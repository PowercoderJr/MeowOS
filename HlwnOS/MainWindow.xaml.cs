using HlwnOS.FileSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HlwnOS
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FileSystemController fsctrl;
        UserInfo.Roles role;
        
        public MainWindow(string path, UserInfo.Roles role)
        {
            InitializeComponent();
            try
            {
                fsctrl = new FileSystemController();
                fsctrl.openSpace(path);
            }
            catch
            {
                //TODO
            }

            this.role = role;
            //TODO 18.11: менять функционал для админа/пользователя

            wrapPanel.Children.Add(new FileView(fsctrl.getFileHeader("/", "users", "sys")));
            wrapPanel.Children.Add(new FileView(fsctrl.getFileHeader("/", "groups", "sys")));
            wrapPanel.Children.Add(new FileView(fsctrl.getFileHeader("/", "kek1")));
        }

        private void openFolder()
        {

        }

        private void printSpace()
        {
            Console.WriteLine(fsctrl.SuperBlock.ToString() + '\n');
            Console.WriteLine(fsctrl.Fat.ToString() + '\n');
            FileHeader test = new FileHeader("testing", "chk", (byte)(FileHeader.FlagsList.FL_HIDDEN | FileHeader.FlagsList.FL_SYSTEM), 1, 1);
            Console.WriteLine(test.ToString() + '\n');
        }

        private void logout(object sender, RoutedEventArgs e)
        {
            fsctrl.closeSpace();
            Session.clear();
            AuthWindow aw = new AuthWindow();
            Close();
            aw.Show();
        }
    }
}
