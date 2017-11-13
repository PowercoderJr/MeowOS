using HlwnOS.FileSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlwnOS.FileSystem
{
    class FileManager
    {
        private enum Areas { SUPERBLOCK, FAT1, FAT2, ROOTDIR, DATA }

        public const ushort FACTOR = 1024;
        public const ushort CLUSTER_SIZE = FACTOR * 4;

        private string path;
        private BinaryWriter bw;
        private SuperBlock superBlock;
        public SuperBlock SuperBlock
        {
            get { return superBlock; }
            set { superBlock = value; }
        }
        private FAT fat;
        public FAT Fat
        {
            get { return fat; }
            set { fat = value; }
        }
        private string rootDir;
        public string RootDir
        {
            get { return rootDir; }
            set { rootDir = value; }
        }

        public FileManager()
        {
            ;
        }

        public void createSpace(string path)
        {
            this.path = path;
            bw = new BinaryWriter(System.IO.File.Create(path));

            //Системная область
            bw.Write(superBlock.toByteArray(true));
            bw.Write(fat.toByteArray(true));
            bw.Write(fat.toByteArray(true));

            //Корневой каталог
            bw.Write(rootDir.ToArray());
            /*if (superBlock.RootSize % superBlock.ClusterSize == 0)
                for (int i = 0; i < superBlock.RootSize / superBlock.ClusterSize; ++i)
                    bw.Write(emptyCluster);
            else
                for (int i = 0; i < superBlock.RootSize; ++i)
                    bw.Write((byte)0);*/
            //Область данных
            byte[] emptyCluster = new byte[superBlock.ClusterSize];
            if ((superBlock.DiskSize - superBlock.DataOffset) % superBlock.ClusterSize == 0)
                for (int i = 0; i < (superBlock.DiskSize - superBlock.DataOffset) / superBlock.ClusterSize; ++i)
                    bw.Write(emptyCluster);
            else
                for (int i = 0; i < (superBlock.DiskSize - superBlock.DataOffset); ++i)
                    bw.Write((byte)0);

            //Заголовок файла с учётными записями пользователей
            FileHeader users = new FileHeader("users", "sys", (byte)(FileHeader.FlagsList.FL_HIDDEN | FileHeader.FlagsList.FL_SYSTEM), 1, 1);
            FileHeader users2 = new FileHeader("users2", "sys", (byte)(FileHeader.FlagsList.FL_HIDDEN | FileHeader.FlagsList.FL_SYSTEM), 1, 1);
            //users.Data = "admin\nadmin\nadmin\nguest\n\nuser";
            string kbyte = "begin56789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCend";
            writeFile("/", users, Encoding.ASCII.GetBytes(kbyte + kbyte + kbyte + kbyte + kbyte + kbyte + kbyte + kbyte + kbyte + kbyte + kbyte + kbyte + kbyte + kbyte + kbyte + kbyte)); //16KB
            writeFile("/", users2, Encoding.ASCII.GetBytes(kbyte + kbyte + kbyte + kbyte + kbyte)); //5KB
            deleteFile(users);
            writeFile("/", users, Encoding.ASCII.GetBytes(kbyte + kbyte + kbyte + kbyte + kbyte + kbyte)); //6KB
        }

        public void closeSpace()
        {
            bw.Close();
        }
        
        public int writeFile(string path, FileHeader header, byte[] data)
        {
            writeArea(Areas.FAT2);
            header.FirstCluster = fat.getFreeClusterIndex();
            header.Size = (uint)data.Length;

            //Запись заголовка
            //Тут использовать путь из path
            rootDir = header.toByteArray(false) + rootDir.Remove(0, FileHeader.SIZE);
            writeArea(Areas.ROOTDIR);

            //Запись данных
            int toWrite = data.Length, i = 0, offset, toWriteOnThisStep;
            ushort currCluster;
            List<ushort> usedClusters = new List<ushort>();
            while (toWrite > 0)
            {
                currCluster = fat.getFreeClusterIndex();
                if (currCluster < fat.TableSize)
                {
                    usedClusters.Add(currCluster);
                    fat.Table[currCluster] = FAT.CL_WRITING;
                    toWriteOnThisStep = Math.Min(superBlock.ClusterSize, toWrite);
                    offset = currCluster * superBlock.ClusterSize;
                    bw.Seek(offset, SeekOrigin.Begin);
                    bw.Write(data, superBlock.ClusterSize * i++, toWriteOnThisStep);
                    toWrite -= toWriteOnThisStep;
                }
                else
                {
                    for (i = 0; i < usedClusters.Count; ++i)
                        fat.Table[usedClusters[i]] = FAT.CL_FREE;
                    return 1;
                }
            }
            for (i = 0; i < usedClusters.Count - 1; ++i)
                fat.Table[usedClusters[i]] = usedClusters[i + 1];
            fat.Table[usedClusters.Last()] = FAT.CL_EOF;
            writeArea(Areas.FAT1);
            return 0;
        }

        public void deleteFile(FileHeader file)
        {
            int currCluster = file.FirstCluster;
            if (currCluster < superBlock.DataOffset / superBlock.ClusterSize)
                return; //Попытка обнулить системные блоки или блоки корневого каталога

            writeArea(Areas.FAT2);
            int nextCluster;
            while (fat.Table[currCluster] != FAT.CL_EOF)
            {
                nextCluster = fat.Table[currCluster];
                fat.Table[currCluster] = FAT.CL_FREE;
                currCluster = nextCluster;
            }
            fat.Table[currCluster] = FAT.CL_FREE;
            writeArea(Areas.FAT1);
        }

        private void writeArea(Areas area)
        {
            uint offset;
            byte[] buffer;
            switch (area)
            {
                case Areas.FAT1:
                    buffer = fat.toByteArray(true);
                    offset = superBlock.Fat1Offset;
                    break;
                case Areas.FAT2:
                    buffer = fat.toByteArray(true);
                    offset = superBlock.Fat2Offset;
                    break;
                case Areas.ROOTDIR:
                    buffer = Encoding.ASCII.GetBytes(rootDir);
                    offset = superBlock.RootOffset;
                    break;
                case Areas.SUPERBLOCK:
                default:
                    buffer = superBlock.toByteArray(true);
                    offset = 0;
                    break;
            }
            bw.Seek((int)offset, SeekOrigin.Begin);
            bw.Write(buffer);
        }
    }
}
