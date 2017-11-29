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

namespace MeowOS.FileSystem
{
    /// <summary>
    /// Логика взаимодействия для FileViewerWindow.xaml
    /// </summary>
    public partial class FileViewerWindow : Window
    {
        private bool isChanged;
        public bool IsChanged => isChanged;

        public FileViewerWindow(FileHeader fh, string content)
        {
            InitializeComponent();
            Title = fh.NamePlusExtensionWithoutZeros;
            textField.Text = content;
            isChanged = false;
        }

        private void textField_TextChanged(object sender, TextChangedEventArgs e)
        {
            isChanged = true;
        }
    }
}
