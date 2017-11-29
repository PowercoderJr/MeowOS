using MeowOS.FileSystem;
using MeowOS.FileSystem.Exceptions;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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

namespace MeowOS
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FileSystemController fsctrl;
        private byte[] bufferFH, bufferData;
        private string bufferRestorePath;
        private FileView selection;

        public MainWindow(string path)
        {
            InitializeComponent();
            try
            {
                fsctrl = new FileSystemController();
                fsctrl.openSpace(path);
                openPath(fsctrl.CurrDir);
            }
            catch
            {

            }
            
            bufferFH = null;
            bufferData = null;
            bufferRestorePath = null;
            Title = "MeowOS - " + Session.userInfo.Login;
            
            showHiddenChb.IsEnabled = Session.userInfo.Role == UserInfo.Roles.ADMIN;
        }

        //private enum PathTypes { PT_ABSOLUTE, PT_RELATIVE };
        private void openPath(string path/*, PathTypes pathType = PathTypes.PT_ABSOLUTE*/)
        {
            path = UsefulThings.clearExcessSeparators(path);
            try
            {
                path = UsefulThings.clearExcessSeparators(path);
                string tmpPathWithoutLast, tmpLast;
                UsefulThings.detachLastFilename(path, out tmpPathWithoutLast, out tmpLast);
                uint newCluster;
                FileHeader fh;
                if (path.Equals(fsctrl.CurrDir))
                {
                    newCluster = fsctrl.CurrDirCluster;
                    fh = new FileHeader("", "", (byte)FileHeader.FlagsList.FL_DIRECTORY, 0, 0); //требуется лишь только, чтобы у заголовка был флаг "директория"
                }
                else if (tmpPathWithoutLast.Equals(fsctrl.CurrDir))
                {
                    FileHeader tmpFH = fsctrl.getFileHeader(tmpLast, fsctrl.CurrDirCluster, true);
                    if (tmpFH == null)
                        throw new InvalidPathException(path);
                    newCluster = tmpFH.FirstCluster;
                    fh = fsctrl.getFileHeader(tmpLast, fsctrl.CurrDirCluster, true);
                }
                else
                {
                    if (path.Equals(""))
                        newCluster = fsctrl.SuperBlock.RootOffset / fsctrl.SuperBlock.ClusterSize;
                    else
                    {
                        FileHeader tmpFH = fsctrl.getFileHeader(path, true);
                        if (tmpFH == null)
                            throw new InvalidPathException(path);
                        newCluster = tmpFH.FirstCluster;
                    }
                    fh = fsctrl.getFileHeader(path, true);
                }

                if (fh == null)
                    throw new InvalidPathException(path);
                if (path.Equals("") || fh.IsDirectory)
                {
                    byte[] dir = fsctrl.readFile(path, true);
                    wrapPanel.Children.Clear();
                    while (dir.Length > 0)
                    {
                        FileHeader currFH = new FileHeader(dir);
                        addFileView(currFH);
                        dir = dir.Skip(FileHeader.SIZE).ToArray();
                    }
                    fsctrl.CurrDir = path;
                    fsctrl.CurrDirCluster = newCluster;
                    addressEdit.Text = fsctrl.CurrDir.Length > 0 ? fsctrl.CurrDir : "/";
                    selection = null;
                    if (fsctrl.CurrDir.Equals(""))
                        backImg.IsEnabled = false;
                    else
                        backImg.IsEnabled = true;
                }
                else
                {
                    FileViewerWindow fvw = new FileViewerWindow(fh, UsefulThings.ENCODING.GetString(fsctrl.readFile(fh, true)));
                    fvw.Title = fh.NamePlusExtensionWithoutZeros;
                    fvw.ShowDialog();
                    if (fvw.IsChanged && MessageBox.Show("Файл был изменён. Сохранить изменения?", "Подтвердите действие",
                        MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                        fsctrl.rewriteFile(fsctrl.CurrDir, fh, UsefulThings.ENCODING.GetBytes(fvw.textField.Text), true);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                openPath(fsctrl.CurrDir);
            }
        }

        private FileView addFileView(FileHeader fh)
        {
            FileView fv = null;
            if (fh.Name.First() != UsefulThings.DELETED_MARK) //Проверять внутри метода или каждый раз перед вызовом?
            {
                if (!fh.IsHidden || fh.IsHidden && showHiddenChb.IsChecked.Value)
                {
                    fv = new FileView(fh);
                    fv.PreviewMouseDown += onFileViewMouseDown;
                    fv.MouseDoubleClick += onFileViewDoubleClick;
                    wrapPanel.Children.Add(fv);
                }
            }
            return fv;
        }

        public void onFileViewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (selection != null)
                selection.panel.Background = Brushes.Transparent;
            FileView senderFV = sender as FileView;
            selection = senderFV;
            senderFV.panel.Background = FileView.selectionBrush;
        }

        private void onFileViewDoubleClick(object sender, MouseButtonEventArgs e)
        {
            FileView senderFV = (sender as FileView);
            openPath(fsctrl.CurrDir + "/" + senderFV.FileHeader.NamePlusExtensionWithoutZeros);
        }

        private void onBackLMBUp(object sender, MouseButtonEventArgs e)
        {
            string newPath, tmp;
            UsefulThings.detachLastFilename(fsctrl.CurrDir, out newPath, out tmp);
            openPath(newPath);
        }

        private void MenuItem_UsersManager_Click(object sender, RoutedEventArgs ea)
        {
            byte[] usersData = fsctrl.readFile("/users.sys", false);
            byte[] groupsData = fsctrl.readFile("/groups.sys", false);
            UsersManagerWindow umw = new UsersManagerWindow(usersData, groupsData);
            umw.ShowDialog();

            try
            {
                if (!umw.UsersData.SequenceEqual(usersData))
                {
                    FileHeader usersHeader = fsctrl.getFileHeader("/users.sys", false);
                    fsctrl.rewriteFile("/", usersHeader, umw.UsersData, false);
                }

                if (!umw.GroupsData.SequenceEqual(groupsData))
                {
                    FileHeader groupsHeader = fsctrl.getFileHeader("/groups.sys", false);
                    fsctrl.rewriteFile("/", groupsHeader, umw.GroupsData, false);
                }

                Title = "MeowOS - " + Session.userInfo.Login;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MenuItem_fsProperties_Click(object sender, RoutedEventArgs e)
        {
            new FileSystemPropertiesWindow(fsctrl.SuperBlock).ShowDialog();
        }

        private void logout(object sender, RoutedEventArgs e)
        {
            fsctrl.closeSpace();
            Session.clear();
            AuthWindow aw = new AuthWindow();
            Close();
            aw.Show();
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                openPath(addressEdit.Text);
                addressEdit.SelectionStart = addressEdit.Text.Length;
            }
        }

        private void MenuItem_Create_Click(object sender, RoutedEventArgs e)
        {
            createCmd((sender as Control) == crDirItem);
        }

        private void createCmd(bool isDirectory)
        {
            FileHeader fh = new FileHeader(Session.userInfo);
            fh.IsDirectory = isDirectory;
            if (isDirectory)
            {
                fh.Name = "newdir";
                fh.Extension = "";
            }

            FilePropertiesWindow fpw = new FilePropertiesWindow(fh);
            if (fpw.ShowDialog().Value)
            {
                try
                {
                    fsctrl.writeFile(fsctrl.CurrDir, fh, null, true);
                    FileView fv = addFileView(fh);
                    onFileViewMouseDown(fv, null);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    if (e is RootdirOutOfSpaceException || e is DiskOutOfSpaceException)
                    {
                        try
                        {
                            fsctrl.deleteFile(fsctrl.CurrDir, fh, false);
                        }
                        catch
                        {
                            //ignore
                        }
                    }
                }
            }
        }
        
        private void MenuItem_Open_Click(object sender, RoutedEventArgs e)
        {
            openCmd();
        }

        private void openCmd()
        {
            onFileViewDoubleClick(selection, null);
        }

        private void MenuItem_Delete_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Вы действительно хотите удалить " + (selection.FileHeader.IsDirectory ? "директорию" : "файл")
                + " \"" + fsctrl.CurrDir + "/" + selection.FileHeader.NamePlusExtensionWithoutZeros + "\"?", "Подтвердите действие",
                MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                deleteCmd();
            }
        }

        private void deleteCmd()
        {
            fsctrl.deleteFile(fsctrl.CurrDir, selection.FileHeader, true);
            wrapPanel.Children.Remove(selection);
            selection = null;
        }

        private void MenuItem_Copy_Click(object sender, RoutedEventArgs e)
        {
            copyCmd();
        }

        private void copyCmd()
        {
            writeToBuffer(selection.FileHeader.toByteArray(false), fsctrl.readFile(selection.FileHeader, false), null);
        }

        private void MenuItem_Cut_Click(object sender, RoutedEventArgs e)
        {
            cutCmd();
        }

        private void cutCmd()
        {
            writeToBuffer(selection.FileHeader.toByteArray(false), null, fsctrl.CurrDir);
        }

        private void writeToBuffer(byte[] fh, byte[] data, string restorePath)
        {
            bufferFH = fh;
            bufferData = data;
            bufferRestorePath = restorePath;
        }

        private void MenuItem_Paste_Click(object sender, RoutedEventArgs e)
        {
            if (bufferFH != null)
                pasteCmd();
            else
                MessageBox.Show("В буфере нет информации", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void pasteCmd()
        {
            bool success = false;
            FileHeader fh = new FileHeader(bufferFH);
            if (bufferRestorePath != null) //вставка вырезанного
            {
                try
                {
                    fsctrl.writeHeader(fsctrl.CurrDir, fh, true);
                    fsctrl.deleteHeader(bufferRestorePath, fh, true);
                    success = true;
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    try
                    {
                        if (!(e is FileAlreadyExistException))
                            fsctrl.deleteHeader(fsctrl.CurrDir, fh, false);
                        fsctrl.writeHeader(bufferRestorePath, fh, false);
                    }
                    catch
                    {
                        //ignore                
                    }
                }
            }
            else //вставка скопированного
            {
                try
                {
                    writeToDisk(fsctrl.CurrDir, fh, bufferData);
                    success = true;
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    if (e is RootdirOutOfSpaceException || e is DiskOutOfSpaceException)
                    {
                        try
                        {
                            fsctrl.deleteFile(fsctrl.CurrDir, fh, false);
                        }
                        catch
                        {
                            //ignore
                        }
                    }
                }
            }

            if (success)
            {
                FileView fv = addFileView(fh);
                if (fv != null)
                    onFileViewMouseDown(fv, null);
            }

            bufferFH = null;    
            bufferData = null;
            bufferRestorePath = null;
        }

        private void writeToDisk(string path, FileHeader fh, byte[] data)
        {
            if (fh.IsDirectory)
            {
                fsctrl.writeFile(path, fh, /*data*/null, true);
                //byte[] content = fsctrl.readFile(fh, false);
                for (int offset = 0; offset < data.Length; offset += FileHeader.SIZE)
                {
                    FileHeader curr = new FileHeader(data.Skip(offset).ToArray());
                    writeToDisk(path + "/" + fh.NameWithoutZeros, curr, fsctrl.readFile(curr, true));
                }
            }
            else
                fsctrl.writeFile(path, fh, data, true);
        }

        private void MenuItem_Properties_Click(object sender, RoutedEventArgs e)
        {
            propertiesCmd();
        }
        
        private void propertiesCmd()
        {
            int headerOffset = (int)fsctrl.getFileHeaderOffset(selection.FileHeader.NamePlusExtensionWithoutZeros, fsctrl.CurrDirCluster, false);
            FilePropertiesWindow fpw = new FilePropertiesWindow(selection.FileHeader);
            if (fpw.ShowDialog().Value)
            {
                fsctrl.writeBytes(headerOffset, selection.FileHeader.toByteArray(false));
                selection.refresh();
            }
        }

        private void showHiddenChb_Changed(object sender, RoutedEventArgs e)
        {
            openPath(fsctrl.CurrDir);
        }

        private void MenuItem_Upload_Click(object sender, RoutedEventArgs e)
        {
            uploadCmd();
        }

        private void uploadCmd()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog().Value)
            {
                FileHeader fh = new FileHeader(string.Concat(System.IO.Path.GetFileNameWithoutExtension(ofd.SafeFileName).Where(char.IsLetterOrDigit)),
                    string.Concat(System.IO.Path.GetExtension(ofd.SafeFileName).Where(char.IsLetterOrDigit)), 0,
                    Session.userInfo.Uid, Session.userInfo.Gid);
                try
                {
                    byte[] data = File.ReadAllBytes(ofd.FileName);
                    fsctrl.writeFile(fsctrl.CurrDir, fh, data, true);
                    FileView fv = addFileView(fh);
                    onFileViewMouseDown(fv, null);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    if (e is RootdirOutOfSpaceException || e is DiskOutOfSpaceException)
                    {
                        try
                        {
                            fsctrl.deleteFile(fsctrl.CurrDir, fh, false);
                        }
                        catch
                        {
                            //ignore
                        }
                    }
                }
            }
        }

        private void MenuItem_Download_Click(object sender, RoutedEventArgs e)
        {
            downloadCmd();
        }

        private void downloadCmd()
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = selection.FileHeader.NamePlusExtensionWithoutZeros;
            if (sfd.ShowDialog().Value)
            {
                File.WriteAllBytes(sfd.FileName, fsctrl.readFile(selection.FileHeader, true));
            }
        }

        private void MenuItem_File_Expand(object sender, RoutedEventArgs e)
        {
            openItem.IsEnabled = deleteItem.IsEnabled = copyItem.IsEnabled = cutItem.IsEnabled = propertiesItem.IsEnabled = selection != null;
            pasteItem.IsEnabled = bufferFH != null;
            downloadItem.IsEnabled = selection != null && !selection.FileHeader.IsDirectory;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (Window window in OwnedWindows)
                window.Close();
        }
    }
}
