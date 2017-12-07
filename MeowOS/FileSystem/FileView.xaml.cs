using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MeowOS.FileSystem
{
    public partial class FileView : UserControl
    {
        private static readonly BitmapImage fileIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/file.png"));
        private static readonly BitmapImage folderIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/folder.png"));
        public static readonly Brush selectionBrush = new SolidColorBrush(Color.FromArgb(150, 50, 50, 200));

        private FileHeader fileHeader;
        public FileHeader FileHeader => fileHeader;

        public FileView(FileHeader fileHeader)
        {
            this.fileHeader = fileHeader;
            InitializeComponent();
            refresh();
        }

        public void refresh()
        {
            iconImg.Source = fileHeader.IsDirectory ? folderIcon : fileIcon;
            iconImg.Opacity = fileHeader.IsHidden ? 0.25 : 1;
            nameLabel.Content = fileHeader.NamePlusExtensionWithoutZeros;
        }
    }
}
