using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HlwnOS.FileSystem
{
    class SuperBlock : AbstractElement
    {
        //Тип ФС - 6 б
        public const int FS_TYPE_LENGTH = 6;
        private string fsType;
        public string FsType
        {
            get { return fsType; }
            private set { fsType = UsefulThings.setStringLength(value, FS_TYPE_LENGTH, '\0', UsefulThings.Alignments.LEFT); }
        }
        //Размер лог. блока - 2 б
        private ushort clusterSize;
        public ushort ClusterSize
        {
            get { return clusterSize; }
            private set { clusterSize = value; }
        }
        //Размер корневого каталога - 2 б
        private ushort rootSize;
        public ushort RootSize
        {
            get { return rootSize; }
            private set { rootSize = value; }
        }
        //Размер раздела - 4 б
        private uint diskSize;
        public uint DiskSize
        {
            get { return diskSize; }
            private set { diskSize = value; }
        }
        //Смещение FAT1 - 2 б
        private ushort fat1Offset;
        public ushort Fat1Offset
        {
            get { return fat1Offset; }
            private set { fat1Offset = value; }
        }
        //Смещение FAT2 - 2 б
        private ushort fat2Offset;
        public ushort Fat2Offset
        {
            get { return fat2Offset; }
            private set { fat2Offset = value; }
        }
        //Смещение корневого каталога - 4 б
        private uint rootOffset;
        public uint RootOffset
        {
            get { return rootOffset; }
            private set { rootOffset = value; }
        }
        //Смещение области данных - 4 б
        private uint dataOffset;
        public uint DataOffset
        {
            get { return dataOffset; }
            private set { dataOffset = value; }
        }

        public SuperBlock(FileManager fm, string fsType, ushort clusterSize, ushort rootSize, uint diskSize/*, ushort fat1Offset, ushort fat2Offset, uint rootOffset,uint dataOffset*/)
        {
            this.fm = fm;
            FsType = fsType;
            ClusterSize = clusterSize;
            RootSize = rootSize;
            DiskSize = diskSize;
            Fat1Offset = clusterSize;
            Fat2Offset = (ushort)(fat1Offset + clusterSize * Math.Ceiling((double)(2 * diskSize / clusterSize) / clusterSize));
            RootOffset = (uint)(fat2Offset + clusterSize * Math.Ceiling((double)(2 * diskSize / clusterSize) / clusterSize));
            DataOffset = (uint)(rootOffset + rootSize);
        }

        public override byte[] toByteArray(bool expandToCluster)
        {
            ArrayList buffer = new ArrayList(expandToCluster ? clusterSize : FS_TYPE_LENGTH +
                Marshal.SizeOf(clusterSize) +
                Marshal.SizeOf(rootSize) +
                Marshal.SizeOf(diskSize) +
                Marshal.SizeOf(fat1Offset) +
                Marshal.SizeOf(fat2Offset) +
                Marshal.SizeOf(rootOffset) +
                Marshal.SizeOf(dataOffset));

            buffer.AddRange(Encoding.ASCII.GetBytes(fsType));
            buffer.AddRange(BitConverter.GetBytes(clusterSize));
            buffer.AddRange(BitConverter.GetBytes(rootSize));
            buffer.AddRange(BitConverter.GetBytes(diskSize));
            buffer.AddRange(BitConverter.GetBytes(fat1Offset));
            buffer.AddRange(BitConverter.GetBytes(fat2Offset));
            buffer.AddRange(BitConverter.GetBytes(rootOffset));
            buffer.AddRange(BitConverter.GetBytes(dataOffset));

            return buffer.OfType<byte>().ToArray();

            /*
            //Запись информации
            bw.Write(fsType.ToArray());
            bw.Write(clusterSize);
            bw.Write(rootSize);
            bw.Write(diskSize);
            bw.Write(fat1Offset);
            bw.Write(fat2Offset);
            bw.Write(rootOffset);
            bw.Write(dataOffset);

            //Количество занятых байтов
            int total = FS_TYPE_LENGTH +
                Marshal.SizeOf(clusterSize) +
                Marshal.SizeOf(rootSize) +
                Marshal.SizeOf(diskSize) +
                Marshal.SizeOf(fat1Offset) +
                Marshal.SizeOf(fat2Offset) +
                Marshal.SizeOf(rootOffset) +
                Marshal.SizeOf(dataOffset);

            //Заполнение оставшегося места в суперблоке
            for (int i = total; i < clusterSize; ++i)
                bw.Write((byte)0);*/

        }

        public override void fromByteArray(byte[] buffer)
        {
            throw new NotImplementedException();
        }
    }
}
