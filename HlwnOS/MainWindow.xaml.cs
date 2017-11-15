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
        private Controller ctrl;

        public MainWindow()
        {
            InitializeComponent();

            ctrl = new Controller();  
            ushort clusterSize = Controller.FACTOR * 4; //Блок = 4 КБ
            ushort rootSize = (ushort)(clusterSize * 10); //Корневой каталог = 10 блоков
            uint diskSize = 1 * Controller.FACTOR * Controller.FACTOR; //Раздел = 50 МБ (или 1 МБ для тестов)
            ctrl.SuperBlock = new SuperBlock(ctrl, "HlwnFS", clusterSize, rootSize, diskSize);
            ctrl.Fat = new FAT(ctrl, (int)(diskSize / clusterSize));
            ctrl.RootDir = new String('\0', rootSize).ToString();

            try
            {
                ctrl.createSpace("hlwn.fs", "admin", "admin");

                /*ctrl.closeSpace();
                printSpace();
                ctrl.openSpace("hlwn.fs");
                ctrl.closeSpace();
                printSpace();*/
            }
            finally
            {
                ctrl.closeSpace();
            }
        }

        private void printSpace()
        {
            Console.WriteLine(ctrl.SuperBlock.ToString() + '\n');
            /*fm.SuperBlock.fromByteArray(fm.SuperBlock.toByteArray(false));
            Console.WriteLine(fm.SuperBlock.ToString() + '\n');*/

            Console.WriteLine(ctrl.Fat.ToString() + '\n');
            /*fm.Fat.fromByteArray(fm.Fat.toByteArray(false));
            Console.WriteLine(fm.Fat.ToString() + '\n');*/

            FileHeader test = new FileHeader("testing", "chk", (byte)(FileHeader.FlagsList.FL_HIDDEN | FileHeader.FlagsList.FL_SYSTEM), 1, 1);
            Console.WriteLine(test.ToString() + '\n');
            /*test.fromByteArray(test.toByteArray(false));
            Console.WriteLine(test.ToString() + '\n');*/
        }
    }
}
