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
    class Controller
    {
        private enum Areas { SUPERBLOCK, FAT1, FAT2, ROOTDIR, DATA }

        public const ushort FACTOR = 1024;
        public const ushort CLUSTER_SIZE = FACTOR * 4;

        private string path;
        private FileStream space = null;
        private BinaryWriter bw = null;
        private BinaryReader br = null;
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

        public Controller()
        {
            ;
        }

        public void createSpace(string path)
        {
            closeSpace();
            this.path = path;
            space = System.IO.File.Open(path, FileMode.Create, FileAccess.ReadWrite); //TODO 14.11: предупреждать, если файл уже существует
            bw = new BinaryWriter(space);
            br = new BinaryReader(space);

            //Системная область
            bw.Write(superBlock.toByteArray(true));
            bw.Write(fat.toByteArray(true));
            bw.Write(fat.toByteArray(true));

            //Корневой каталог
            bw.Write(rootDir.ToArray());

            //Область данных
            byte[] emptyCluster = new byte[superBlock.ClusterSize];
            uint wholeClustersAmount = (superBlock.DiskSize - superBlock.DataOffset) / superBlock.ClusterSize;
            uint remainder = (superBlock.DiskSize - superBlock.DataOffset) - wholeClustersAmount * superBlock.ClusterSize;
            for (int i = 0; i < wholeClustersAmount; ++i)
                bw.Write(emptyCluster);
            for (int i = 0; i < remainder; ++i)
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

        public void openSpace(string path)
        {
            closeSpace();
            this.path = path;
            space = System.IO.File.Open(path, FileMode.Open, FileAccess.ReadWrite); //TODO 14.11: предупреждать, если файл не найден
            bw = new BinaryWriter(space);
            br = new BinaryReader(space);
            
            //Системная область
            this.SuperBlock = new SuperBlock(this, br.ReadBytes(SuperBlock.SIZE));
            Fat = new FAT(this, (int)superBlock.DiskSize / superBlock.ClusterSize);
            br.BaseStream.Seek(superBlock.Fat1Offset, SeekOrigin.Begin);
            Fat.fromByteArray(br.ReadBytes(Fat.TableSize * FAT.ELEM_SIZE));

            //Корневой каталог
            br.BaseStream.Seek((int)superBlock.RootOffset, SeekOrigin.Begin);
            rootDir = Encoding.ASCII.GetString(br.ReadBytes(superBlock.RootSize));
        }

        public void closeSpace()
        {
            if (space != null)
            {
                space.Flush();
                space.Close();
                space = null;
            }

            if (bw != null)
            {
                bw.Close();
                bw = null;
            }

            if (br != null)
            {
                br.Close();
                br = null;
            }
        }
        
        public int writeFile(string path, FileHeader header, byte[] data)
        {
            writeArea(Areas.FAT2);
            header.FirstCluster = fat.getFreeClusterIndex();
            header.Size = (uint)data.Length;

            //Запись заголовка
            //TODO 14.11: тут использовать путь из path
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
