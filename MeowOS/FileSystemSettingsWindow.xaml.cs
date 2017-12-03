using MeowOS.FileSystem;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MeowOS
{
    /// <summary>
    /// Логика взаимодействия для FileSystemSettingsWindow.xaml
    /// </summary>
    public partial class FileSystemSettingsWindow : Window
    {
        private ushort clusterSizeBytes;
        private ushort rootSizeClusters;
        private uint dataSizeClusters;
        private uint diskSizeBytes;
        public ushort ClusterSizeBytes => clusterSizeBytes;
        public ushort RootSizeBytes => (ushort)(rootSizeClusters * clusterSizeBytes);
        public ushort RootSizeClusters => rootSizeClusters;
        //public uint DataSize => dataSize;
        public uint DiskSizeBytes => diskSizeBytes;

        public FileSystemSettingsWindow()
        {
            InitializeComponent();
            clusterSizeEdit.Text = "4096";
            rootSizeEdit.Text = "10";
            dataSizeEdit.Text = "12775";
            clusterSizeEdit.SelectAll();
        }

        private void fieldTextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            int ss = tb.SelectionStart - 1;
            string text = tb.Text;
            tb.Text = string.Concat(tb.Text.Where(char.IsDigit));

            if (!tb.Text.Equals(text))
                tb.SelectionStart = Math.Max(0, ss);

            okBtn.IsEnabled = clusterSizeEdit.Text.Length > 0 && rootSizeEdit.Text.Length > 0 && dataSizeEdit.Text.Length > 0;
            if (okBtn.IsEnabled)
            {
                try
                {
                    clusterSizeBytes = ushort.Parse(clusterSizeEdit.Text);
                    rootSizeClusters = ushort.Parse(rootSizeEdit.Text);
                    dataSizeClusters = uint.Parse(dataSizeEdit.Text);
                    uint fatWOfatSizeBytes = (uint)(1 + rootSizeClusters + dataSizeClusters) * FAT.ELEM_SIZE;
                    uint fatWOfatSizeClusters = (uint)Math.Ceiling((double)fatWOfatSizeBytes / clusterSizeBytes);
                    uint fatWfatSizeBytes = (uint)(fatWOfatSizeBytes + 2 * fatWOfatSizeClusters * FAT.ELEM_SIZE);
                    uint fatWfatSizeClusters = (uint)Math.Ceiling((double)fatWfatSizeBytes / clusterSizeBytes);
                    diskSizeBytes = (1 + 2 * fatWfatSizeClusters + rootSizeClusters + dataSizeClusters) * clusterSizeBytes;
                    double sizeToShow = diskSizeBytes;
                    string[] dimensions = { "Б", "Кб", "Мб", "Гб" };
                    int i;
                    for (i = 0; i < dimensions.Length && sizeToShow > FileSystemController.FACTOR; ++i)
                        sizeToShow /= FileSystemController.FACTOR;
                    totalSizeLabel.Content = String.Format("Итоговый размер диска: {0:0.#} {1} ({2} байт)", sizeToShow, dimensions[i], diskSizeBytes);
                    totalSizeLabel.Visibility = Visibility.Visible;
                }
                catch
                {
                    okBtn.IsEnabled = false;
                    totalSizeLabel.Visibility = Visibility.Hidden;
                    MessageBox.Show("Введено недопустимое значение", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
                totalSizeLabel.Visibility = Visibility.Hidden;
        }

        private void okBtn_Click(object sender, RoutedEventArgs e)
        {
            int minClusterSize = Math.Max(FileHeader.SIZE, SuperBlock.SIZE);
            if (clusterSizeBytes < minClusterSize)
            {
                MessageBox.Show("Размер блока должен быть не менее " + minClusterSize + " байт",
                    "Укажите входные данные", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (rootSizeClusters < 1)
            {
                MessageBox.Show("Размер корневого каталога должен быть положительным числом",
                    "Укажите входные данные", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            int minDataSize = clusterSizeBytes * rootSizeClusters / FileHeader.SIZE;
            if (dataSizeClusters < minDataSize)
            {
                MessageBox.Show("Размер области данных слишком мал: при заданных размерах блока и корневого каталога минимальный размер области данных составляет " +
                    minDataSize + " блоков",
                    "Укажите входные данные", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
        }

        private void cancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
