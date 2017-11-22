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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MeowOS.FileSystem
{
    //TODO 19.11: обеспечить управление с клавиатуры
    //            добавить контекстное меню
    public partial class FileView : UserControl
    {
        private static readonly BitmapImage fileIcon = new BitmapImage(new Uri("pack://siteoforigin:,,,/Resources/file.png"));
        private static readonly BitmapImage folderIcon = new BitmapImage(new Uri("pack://siteoforigin:,,,/Resources/folder.png"));
        private static readonly Brush selectionBrush = new SolidColorBrush(Color.FromArgb(150, 50, 50, 200));
        public static FileView selection;

        private FileHeader fh;
        public FileHeader FileHeader => fh;

        public FileView(FileHeader fh)
        {
            this.fh = fh;
            InitializeComponent();
            refresh();
        }

        public void onLMBDown(object sender, MouseButtonEventArgs e)
        {
            if (selection != null)
                selection.panel.Background = Brushes.Transparent;
            selection = this;
            panel.Background = selectionBrush;
        }

        public void refresh()
        {
            iconImg.Source = fh.IsDirectory ? folderIcon : fileIcon;
            iconImg.Opacity = fh.IsHidden ? 0.25 : 1;
            nameLabel.Content = fh.NamePlusExtension;
        }
    }
}
