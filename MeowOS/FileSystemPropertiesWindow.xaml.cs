using MeowOS.FileSystem;
using System.Windows;

namespace MeowOS
{
    /// <summary>
    /// Логика взаимодействия для FileSystemPropertiesWindow.xaml
    /// </summary>
    public partial class FileSystemPropertiesWindow : Window
    {
        public FileSystemPropertiesWindow(SuperBlock sb)
        {
            InitializeComponent();
            fsTypeEdit.Text = sb.FsType;
            clusterSizeEdit.Text = sb.ClusterSize.ToString() + " байт";
            rootSizeEdit.Text = sb.RootSize.ToString() + " байт";
            diskSizeEdit.Text = sb.DiskSize.ToString() + " байт";
            fat1OffsetEdit.Text = sb.Fat1Offset.ToString() + " байт";
            fat2OffsetEdit.Text = sb.Fat2Offset.ToString() + " байт";
            rootOffsetEdit.Text = sb.RootOffset.ToString() + " байт";
            dataOffsetEdit.Text = sb.DataOffset.ToString() + " байт";
        }

        private void okClick(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
