using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MeowOS.FileSystem
{
    public class FileHeader : IConvertibleToBytes, IConvertibleFromBytes, ICloneable
    {
        public enum FlagsList { FL_READONLY = 1 << 0, FL_HIDDEN = 1 << 1, FL_SYSTEM = 1 << 2, FL_DIRECTORY = 1 << 3 };
        public enum RightsList
        {
            OX = 1 << 0,
            OW = 1 << 1,
            OR = 1 << 2,
            GX = 1 << 3,
            GW = 1 << 4,
            GR = 1 << 5,
            UX = 1 << 6,
            UW = 1 << 7,
            UR = 1 << 8
        }

        public const int SIZE = 32;
        public const ushort DEFAULT_RIGHTS = (7 << 6) | (5 << 3) | 5;

        //Имя файла - 8 б
        public const int NAME_MAX_LENGTH = 8;
        private string name;
        public string Name
        {
            get => name;
            set => name = UsefulThings.setStringLength(value, NAME_MAX_LENGTH);
        }
        public string NameWithoutZeros => UsefulThings.truncateZeros(name);

        //Расширение - 3 б
        public const int EXTENSION_MAX_LENGTH = 3;
        private string extension;
        public string Extension
        {
            get => extension;
            set => extension = UsefulThings.setStringLength(value, EXTENSION_MAX_LENGTH);
        }
        public string ExtensionWithoutZeros => UsefulThings.truncateZeros(extension);

        public string NamePlusExtensionWithoutZeros => IsDirectory ? NameWithoutZeros : NameWithoutZeros + "." + ExtensionWithoutZeros;
        public string NamePlusExtension => IsDirectory ? Name : Name + "." + Extension;

        //Размер - 4 б
        private uint size;
        public uint Size
        {
            get => size;
            set => size = value;
        }

        //Права доступа - 2 б
        private ushort accessRights;
        public ushort AccessRights
        {
            get => accessRights;
            set => accessRights = value;
        }

        //Флаги - 1 б
        private byte flags;
        public byte Flags
        {
            get => flags;
            set => flags = value;
        }
        public bool IsReadonly
        {
            get => (Flags & (byte)FlagsList.FL_READONLY) > 0;
            set => setFlag(FlagsList.FL_READONLY, value);
        }
        public bool IsHidden
        {
            get => (Flags & (byte)FlagsList.FL_HIDDEN) > 0;
            set => setFlag(FlagsList.FL_HIDDEN, value);
        }
        public bool IsSystem
        {
            get => (Flags & (byte)FlagsList.FL_SYSTEM) > 0;
            set => setFlag(FlagsList.FL_SYSTEM, value);
        }
        public bool IsDirectory
        {
            get => (Flags & (byte)FlagsList.FL_DIRECTORY) > 0;
            set => setFlag(FlagsList.FL_DIRECTORY, value);
        }
        private void setFlag(FlagsList flag, bool value)
        {
            if (value)
                Flags |= (byte)flag;
            else
                Flags &= (byte)~(byte)flag;
        }

        //ID пользователя - 2 б
        private ushort uid;
        public ushort Uid
        {
            get => uid;
            set => uid = value;
        }

        //ID группы - 2 б
        private ushort gid;
        public ushort Gid
        {
            get => gid;
            set => gid = value;
        }

        //Номер начального кластера - 2 б
        private ushort firstCluster;
        public ushort FirstCluster
        {
            get => firstCluster;
            set => firstCluster = value;
        }

        //Дата изменения - 2 б
        private ushort chDate;
        public ushort ChDate
        {
            get => chDate;
            set => chDate = value;
        }
        public string ChDateDDMMYYYY
        {
            get
            {
                string day = ((ushort)((ushort)(chDate << 11) >> 11)).ToString();
                if (day.Length == 1) day = "0" + day;
                string month = ((ushort)((ushort)(chDate << 7) >> 12)).ToString();
                if (month.Length == 1) month = "0" + month;
                string year = ((ushort)(chDate >> 9) + 1980).ToString();
                return day + "." + month + "." + year;
            }
        }

        //Время изменения - 2 б
        private ushort chTime;
        public ushort ChTime
        {
            get => chTime;
            set => chTime = value;
        }
        public string ChTimeHHMMSS
        {
            get
            {
                string hours = ((ushort)(chTime >> 11)).ToString();
                if (hours.Length == 1) hours = "0" + hours;
                string minutes = ((ushort)((ushort)(chTime << 5) >> 10)).ToString();
                if (minutes.Length == 1) minutes = "0" + minutes;
                string seconds = ((ushort)((ushort)(chTime << 11) >> 11) * 2).ToString();
                if (seconds.Length == 1) seconds = "0" + seconds;
                return hours + ":" + minutes + ":" + seconds;
            }
        }

        //Зарезервировано - 4 б
        public const uint reserved = 0;

        public FileHeader() {}

        /// <summary>
        /// Создаёт заголовок файла с параметрами по умолчанию от имени заданного пользователя
        /// </summary>
        /// <param name="userInfo">Пользователь</param>
        public FileHeader(UserInfo userInfo)
        {
            Name = "newfile";
            Extension = "ext";
            Size = 0;
            AccessRights = DEFAULT_RIGHTS;
            Flags = 0;
            Uid = userInfo.Uid;
            Gid = userInfo.Gid;
            FirstCluster = 0;
            DateTime now = DateTime.Now;
            ChDate = dateToUshort(now);
            ChTime = timeToUshort(now);
        }

        /// <summary>
        /// Создаёт заголовок файла на основе считанной информации 
        /// </summary>
        /// <param name="input">Входной поток</param>
        public FileHeader(Stream input)
        {
            fromByteStream(input);
        }

        /// <summary>
        /// Создаёт заголовок файла на основе считанной информации 
        /// </summary>
        /// <param name="input">Источник</param>
        public FileHeader(byte[] input)
        {
            fromByteArray(input);
        }

        /// <summary>
        /// Создаёт заголовок файла с заданными параметрами
        /// </summary>
        /// <param name="name">Имя файла</param>
        /// <param name="extension">Расширение файла</param>
        /// <param name="flags">Атрибуты</param>
        /// <param name="uid">ID пользователя</param>
        /// <param name="gid">ID группы пользователя</param>
        public FileHeader(string name, string extension, byte flags, ushort uid, ushort gid)
        {
            Name = name;
            Extension = extension;
            Size = 0;
            AccessRights = DEFAULT_RIGHTS;
            Flags = flags;
            Uid = uid;
            Gid = gid;
            FirstCluster = 0;
            DateTime now = DateTime.Now;
            ChDate = dateToUshort(now);
            ChTime = timeToUshort(now);
        }

        private static ushort dateToUshort(DateTime date)
        {
            return (ushort)(((date.Year - 1980) << 9) + (date.Month << 5) + date.Day);
        }

        private static ushort timeToUshort(DateTime time)
        {
            return (ushort)((time.Hour << 11) + (time.Minute << 5) + time.Second / 2);
        }
        
        public byte[] toByteArray(bool expandToCluster)
        {
            ArrayList buffer = new ArrayList(SIZE);
            buffer.AddRange(UsefulThings.ENCODING.GetBytes(name.ToArray()));
            buffer.AddRange(UsefulThings.ENCODING.GetBytes(extension.ToArray()));
            buffer.AddRange(BitConverter.GetBytes(size));
            buffer.AddRange(BitConverter.GetBytes(accessRights));
            buffer.Add(flags);
            buffer.AddRange(BitConverter.GetBytes(uid));
            buffer.AddRange(BitConverter.GetBytes(gid));
            buffer.AddRange(BitConverter.GetBytes(firstCluster));
            buffer.AddRange(BitConverter.GetBytes(chDate));
            buffer.AddRange(BitConverter.GetBytes(chTime));
            buffer.AddRange(BitConverter.GetBytes(reserved));
            return buffer.OfType<byte>().ToArray();
        }

        public void fromByteArray(byte[] buffer)
        {
            int offset = 0;
            Name = UsefulThings.ENCODING.GetString(buffer, offset, NAME_MAX_LENGTH);
            offset = NAME_MAX_LENGTH;
            Extension = UsefulThings.ENCODING.GetString(buffer, offset, EXTENSION_MAX_LENGTH);
            offset += EXTENSION_MAX_LENGTH;
            Size = BitConverter.ToUInt32(buffer, offset);
            offset += sizeof(uint);
            AccessRights = BitConverter.ToUInt16(buffer, offset);
            offset += sizeof(ushort);
            Flags = (byte)BitConverter.ToChar(buffer, offset);
            offset += sizeof(byte);
            Uid = BitConverter.ToUInt16(buffer, offset);
            offset += sizeof(ushort);
            Gid = BitConverter.ToUInt16(buffer, offset);
            offset += sizeof(ushort);
            FirstCluster = BitConverter.ToUInt16(buffer, offset);
            offset += sizeof(ushort);
            ChDate = BitConverter.ToUInt16(buffer, offset);
            offset += sizeof(ushort);
            ChTime = BitConverter.ToUInt16(buffer, offset);
        }

        public void fromByteStream(Stream input)
        {
            BinaryReader br = new BinaryReader(input);
            fromByteArray(br.ReadBytes(SIZE));
        }

        public override string ToString()
        {
            return "Name: " + name +
                "\nExtension: " + extension + 
                "\nSize: " + size + 
                "\nAccess rights: " + accessRights + 
                "\nFlags: " + flags + 
                "\nUid: " + uid + 
                "\nGid: " + gid + 
                "\nFirst cluster: " + firstCluster + 
                "\nChange date: " + chDate + 
                "\nChange time: " + chTime;
        }

        public object Clone()
        {
            return new FileHeader(toByteArray(false));
        }
    }
}
