using System.Windows;
using System.Windows.Controls;

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
