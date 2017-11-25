using MeowOS.FileSystem;
using MeowOS.FileSystem.Exceptions;
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

namespace MeowOS
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private FileSystemController fsctrl;
        private UserInfo.Roles role;
        private byte[] bufferFH, bufferData;
        private string bufferRestorePath;
        private delegate void OperationOnSelection();

        public MainWindow(string path, UserInfo.Roles role)
        {
            InitializeComponent();
            try
            {
                fsctrl = new FileSystemController();
                fsctrl.openSpace(path);
                openDirectory(fsctrl.CurrDir);
            }
            catch
            {
                //TODO
            }

            this.role = role;
            bufferFH = null;
            bufferData = null;
            bufferRestorePath = null;
            Title = "MeowOS - " + Session.userInfo.Login;
            //TODO 18.11: менять функционал для админа/пользователя
        }

        private void openDirectory(string path)
        {
            wrapPanel.Children.Clear();
            path = UsefulThings.clearExcessSeparators(path);

            try
            {
                byte[] dir = fsctrl.readFile(path);
                while (dir.Length > 0)
                {
                    FileHeader fh = new FileHeader(dir);
                    addFileView(fh);
                    dir = dir.Skip(FileHeader.SIZE).ToArray();
                }
                fsctrl.CurrDir = path;
                addressEdit.Text = fsctrl.CurrDir;
                FileView.selection = null;
                if (fsctrl.CurrDir == "/")
                    backImg.IsEnabled = false;
                else
                    backImg.IsEnabled = true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                openDirectory(fsctrl.CurrDir);
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
                    fv.MouseDoubleClick += new MouseButtonEventHandler(onFileViewDoubleClick);
                    wrapPanel.Children.Add(fv);
                }
            }
            return fv;
        }

        private void onFileViewDoubleClick(object sender, MouseButtonEventArgs e)
        {
            FileView senderFV = (sender as FileView);
            if (senderFV.FileHeader.IsDirectory)
            {
                openDirectory(fsctrl.CurrDir + "/" + senderFV.FileHeader.NameWithoutZeros);
            }
            else
            {
                new FileViewerWindow(UsefulThings.ENCODING.GetString(fsctrl.readFile(senderFV.FileHeader))).ShowDialog();
            }
        }

        private void onBackLMBUp(object sender, MouseButtonEventArgs e)
        {
            string newPath, tmp;
            UsefulThings.detachLastFilename(fsctrl.CurrDir, out newPath, out tmp);
            openDirectory(newPath);
        }

        private void MenuItem_UsersManager_Click(object sender, RoutedEventArgs e)
        {
            byte[] usersData = fsctrl.readFile("/users.sys");
            byte[] groupsData = fsctrl.readFile("/groups.sys");
            UsersManagerWindow umw = new UsersManagerWindow(usersData, groupsData);
            umw.ShowDialog();

            if (!umw.UsersData.SequenceEqual(usersData))
            {
                FileHeader usersHeader = fsctrl.getFileHeader("/users.sys");
                fsctrl.deleteFile("/", usersHeader);
                fsctrl.writeFile("/", usersHeader, umw.UsersData);
            }

            if (!umw.GroupsData.SequenceEqual(groupsData))
            {
                FileHeader groupsHeader = fsctrl.getFileHeader("/groups.sys");
                fsctrl.deleteFile("/", groupsHeader);
                fsctrl.writeFile("/", groupsHeader, umw.GroupsData);
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
                openDirectory(addressEdit.Text);
                addressEdit.SelectionStart = addressEdit.Text.Length;
            }
        }

        private void MenuItem_Create_Click(object sender, RoutedEventArgs e)
        {
            createCmd((sender as Control) == crDirItem);
        }

        private void createCmd(bool isDirectory)
        {
            //TODO: 22.11: проверить права записи
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
                    fsctrl.writeFile(fsctrl.CurrDir, fh, null);
                    FileView fv = addFileView(fh);
                    fv.onLMBDown(fv, null);
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        
        private void MenuItem_Open_Click(object sender, RoutedEventArgs e)
        {
            operationOnSelection(openCmd);
        }

        private void openCmd()
        {
            onFileViewDoubleClick(FileView.selection, null);
        }

        private void MenuItem_Delete_Click(object sender, RoutedEventArgs e)
        {
            operationOnSelection(deleteCmd);
        }

        private void deleteCmd()
        {
            fsctrl.deleteFile(fsctrl.CurrDir, FileView.selection.FileHeader);
            wrapPanel.Children.Remove(FileView.selection);
            FileView.selection = null;
        }

        private void MenuItem_Copy_Click(object sender, RoutedEventArgs e)
        {
            operationOnSelection(copyCmd);
        }

        private void copyCmd()
        {
            //TODO 22.11: копировать также вложенные файлы
            bufferFH = FileView.selection.FileHeader.toByteArray(false);
            bufferData = fsctrl.readFile(FileView.selection.FileHeader);
            bufferRestorePath = null;
        }

        private void MenuItem_Cut_Click(object sender, RoutedEventArgs e)
        {
            operationOnSelection(cutCmd);
        }

        private void cutCmd()
        {
            copyCmd();
            deleteCmd();
            bufferRestorePath = fsctrl.CurrDir;
        }

        private void MenuItem_Paste_Click(object sender, RoutedEventArgs e)
        {
            if (bufferFH != null && bufferData != null)
                pasteCmd();
            else
                MessageBox.Show("В буфере нет информации", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void pasteCmd()
        {
            try
            {
                FileHeader fh = new FileHeader(bufferFH);
                fsctrl.writeFile(fsctrl.CurrDir, fh, bufferData);
                FileView fv = addFileView(fh);
                if (fv != null)
                    fv.onLMBDown(fv, null);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                if (e is FileAlreadyExistException && bufferRestorePath != null)
                {
                    try
                    {
                        FileHeader fh = new FileHeader(bufferFH);
                        fsctrl.writeFile(bufferRestorePath, fh, bufferData);
                    }
                    catch
                    {
                        //ignore
                    }
                }
            }
            finally
            {
                bufferFH = null;
                bufferData = null;
                bufferRestorePath = null;
            }
        }

        private void MenuItem_Properties_Click(object sender, RoutedEventArgs e)
        {
            operationOnSelection(propertiesCmd);
        }
        
        private void propertiesCmd()
        {
            int headerOffset = (int)fsctrl.getFileHeaderOffset(fsctrl.CurrDir + "/" + FileView.selection.FileHeader.NamePlusExtension);
            FilePropertiesWindow fpw = new FilePropertiesWindow(FileView.selection.FileHeader);
            if (fpw.ShowDialog().Value)
            {
                fsctrl.writeBytes(headerOffset, FileView.selection.FileHeader.toByteArray(false));
                FileView.selection.refresh();
            }
        }

        private void showHiddenChb_Changed(object sender, RoutedEventArgs e)
        {
            openDirectory(fsctrl.CurrDir);
        }

        private void operationOnSelection(OperationOnSelection operation)
        {
            if (FileView.selection != null)
                operation(); 
            else
                MessageBox.Show("Нет выделения", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            foreach (Window window in OwnedWindows)
                window.Close();
        }
    }
}
