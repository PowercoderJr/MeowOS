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
    /// Логика взаимодействия для FilePropertiesWindow.xaml
    /// </summary>
    public partial class FilePropertiesWindow : Window
    {
        //TODO 28.11: добавить поля UID и GID владельца
        private FileHeader fh;
        public FileHeader FH => fh;
        private CheckBox[] flagsChbs;
        private CheckBox[] accessRightsChbs;

        public FilePropertiesWindow(FileHeader fh)
        {
            InitializeComponent();

            this.fh = fh;
            flagsChbs = new CheckBox[3];
            flagsChbs[0] = flReadOnlyChb;
            flagsChbs[1] = flHiddenChb;
            flagsChbs[2] = flSystemChb;
            accessRightsChbs = new CheckBox[9];
            accessRightsChbs[0] = oxChb;
            accessRightsChbs[1] = owChb;
            accessRightsChbs[2] = orChb;
            accessRightsChbs[3] = gxChb;
            accessRightsChbs[4] = gwChb;
            accessRightsChbs[5] = grChb;
            accessRightsChbs[6] = uxChb;
            accessRightsChbs[7] = uwChb;
            accessRightsChbs[8] = urChb;

            Title = (fh.IsDirectory ? fh.NameWithoutZeros : fh.NameWithoutZeros + "." + fh.ExtensionWithoutZeros) + " - свойства";
            if (fh.IsDirectory)
                extensionEdit.IsEnabled = false;
            nameEdit.MaxLength = FileHeader.NAME_MAX_LENGTH;
            extensionEdit.MaxLength = FileHeader.EXTENSION_MAX_LENGTH;

            //Заполнение полей
            nameEdit.Text = fh.NameWithoutZeros;
            extensionEdit.Text = fh.ExtensionWithoutZeros;
            sizeEdit.Text = fh.Size.ToString() + " байт";
            chDateEdit.Text = fh.ChDateDDMMYYYY;
            chTimeEdit.Text = fh.ChTimeHHMMSS;
            uidEdit.Text = fh.Uid.ToString();
            gidEdit.Text = fh.Gid.ToString();
            for (int i = 0; i < flagsChbs.Length; ++i)
                flagsChbs[i].IsChecked = (fh.Flags & (1 << i)) > 0;
            for (int i = 0; i < accessRightsChbs.Length; ++i)
                accessRightsChbs[i].IsChecked = (fh.AccessRights & (1 << i)) > 0;


            okBtn.IsEnabled = (Session.userInfo == null || Session.userInfo.Role == UserInfo.Roles.ADMIN ||
                (fh.AccessRights & (ushort)FileHeader.RightsList.OW) > 0 ||
                (fh.AccessRights & (ushort)FileHeader.RightsList.GW) > 0 && fh.Gid == Session.userInfo.Gid ||
                (fh.AccessRights & (ushort)FileHeader.RightsList.UW) > 0 && fh.Uid == Session.userInfo.Uid);                
        }

        private void okBtn_Click(object sender, RoutedEventArgs e)
        {
            fh.Name = nameEdit.Text;
            fh.Extension = extensionEdit.Text;

            for (int i = 0; i < flagsChbs.Length; ++i)
                if (flagsChbs[i].IsChecked.Value)
                    fh.Flags |= (byte)(1 << i);
                else
                    fh.Flags &= (byte)~(1 << i);

            for (int i = 0; i < accessRightsChbs.Length; ++i)
                if (accessRightsChbs[i].IsChecked.Value)
                    fh.AccessRights |= (ushort)(1 << i);
                else
                    fh.AccessRights &= (ushort)~(1 << i);

            DialogResult = true;
        }

        private void cancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
        
        private void nameOrExtensionChanged(object sender, TextChangedEventArgs e)
        {
            UsefulThings.controlLettersAndDigits(sender as TextBox);
            okBtn.IsEnabled = nameEdit.Text.Length > 0 && (fh.IsDirectory || extensionEdit.Text.Length > 0);
        }
    }
}
