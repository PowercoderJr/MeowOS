using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Old.HlwnOS.FileSystem
{
    class File
    {
        /*public enum FlagsList { FL_READONLY = 1 << 0, FL_HIDDEN = 1 << 1, FL_SYSTEM = 1 << 2, FL_DIRECTORY = 1 << 3 };

        public const int HEADER_SIZE = 32;

        //Имя файла - 8 б
        const int NAME_MAX_LENGTH = 8;
        private string name;
        public string Name
        {
            get { return name; }
            set { name = UsefulThings.setStringLength(value, NAME_MAX_LENGTH, '\0', UsefulThings.Alignments.LEFT); }
        }

        //Расширение - 3 б
        const int EXTENSION_MAX_LENGTH = 3;
        private string extension;
        public string Extension
        {
            get { return extension; }
            set { extension = UsefulThings.setStringLength(value, EXTENSION_MAX_LENGTH, '\0', UsefulThings.Alignments.LEFT); }
        }

        //Размер - 4 б
        private uint size;
        public uint Size
        {
            get { return size; }
            set { size = value; }
        }

        //Права доступа - 2 б
        private ushort accessRights;
        public ushort AccessRights
        {
            get { return accessRights; }
            set { accessRights = value; }
        }

        //Флаги - 1 б
        private byte flags;
        public byte Flags
        {
            get { return flags; }
            set { flags = value; }
        }

        //ID пользователя - 2 б
        private ushort uid;
        public ushort Uid
        {
            get { return uid; }
            set { uid = value; }
        }

        //ID группы - 2 б
        private ushort gid;
        public ushort Gid
        {
            get { return gid; }
            set { gid = value; }
        }

        //Номер начального кластера - 2 б
        private ushort firstCluster;
        public ushort FirstCluster
        {
            get { return firstCluster; }
            set { firstCluster = value; }
        }

        //Дата изменения - 2 б
        private ushort chDate;
        public ushort ChDate
        {
            get { return chDate; }
            set { chDate = value; } 
        }

        //Время изменения - 2 б
        private ushort chTime;
        public ushort ChTime
        {
            get { return chTime; }
            set { chTime = value; }
        }

        //Зарезервировано - 4 б
        public const uint reserved = 0;

        //Данные
        private string data;
        public string Data
        {
            get { return data; }
            set { data = value; size = (uint)data.Length; }
        }

        public File(string name, string extension, byte flags, ushort uid, ushort gid, ushort firstCluster)
        {
            Name = name;
            Extension = extension;
            Size = 0;
            Flags = flags;
            Uid = uid;
            Gid = gid;
            FirstCluster = firstCluster;
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
        }*/
    }
}
