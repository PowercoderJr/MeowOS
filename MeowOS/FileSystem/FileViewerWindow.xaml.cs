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
    /// Логика взаимодействия для FileViewerWindow.xaml
    /// </summary>
    public partial class FileViewerWindow : Window
    {
        public FileViewerWindow(string content)
        {
            InitializeComponent();
            textField.Text = content;
        }
    }
}
