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
        public MainWindow()
        {
            InitializeComponent();

            FileManager fm = new FileManager();  
            ushort clusterSize = FileManager.FACTOR * 4; //Блок = 4 КБ
            ushort rootSize = (ushort)(clusterSize * 10); //Корневой каталог = 10 блоков
            uint diskSize = 1 * FileManager.FACTOR * FileManager.FACTOR; //Раздел = 50 МБ
            fm.SuperBlock = new SuperBlock(fm, "HlwnFS", clusterSize, rootSize, diskSize);
            fm.Fat = new FAT(fm, (int)(diskSize / clusterSize));
            fm.RootDir = new String('\0', rootSize).ToString();
            fm.createSpace("hlwn.fs");
            fm.closeSpace();
        }
    }
}
