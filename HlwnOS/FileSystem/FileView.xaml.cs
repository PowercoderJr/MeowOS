﻿using System;
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

namespace HlwnOS.FileSystem
{
    //TODO 19.11: обеспечить управление с клавиатуры
    //            добавить контекстное меню
    public partial class FileView : UserControl
    {
        private static readonly BitmapImage fileIcon = new BitmapImage(new Uri("pack://siteoforigin:,,,/Resources/file.png"));
        private static readonly BitmapImage folderIcon = new BitmapImage(new Uri("pack://siteoforigin:,,,/Resources/folder.png"));
        private static readonly Brush selectionBrush = new SolidColorBrush(Color.FromArgb(150, 50, 50, 200));
        public static FileView selection;

        private bool isDirectory;
        //private Delegate openFileDelegate;

        public FileView(FileHeader fh/*, Delegate openFileDelegate*/)
        {
            InitializeComponent();
            isDirectory = (fh.Flags & (byte)FileHeader.FlagsList.FL_DIRECTORY) > 0;
            string name = fh.Name.Substring(0, fh.Name.IndexOf('\0') < 0 ? fh.Name.Length : fh.Name.IndexOf('\0'));
            string extension = fh.Extension.Substring(0, fh.Extension.IndexOf('\0') < 0 ? fh.Extension.Length : fh.Extension.IndexOf('\0'));
            if (isDirectory)
            {
                iconImg.Source = folderIcon;
                nameLabel.Content = name;
            }
            else
            {
                iconImg.Source = fileIcon;
                nameLabel.Content = name + '.' + extension;
            }

            if ((fh.Flags & (byte)FileHeader.FlagsList.FL_HIDDEN) > 0)
                iconImg.Opacity = 0.25;
        }

        private void onMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (selection != null)
                selection.panel.Background = Brushes.Transparent;
            selection = this;
            panel.Background = selectionBrush;
        }
    }
}
