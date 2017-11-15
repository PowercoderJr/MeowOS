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

        public void createSpace(string path, string adminLogin, string adminPassword)
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

            //Заголовок файла с группами пользователей
            FileHeader groupsHeader = new FileHeader("groups", "sys", (byte)(FileHeader.FlagsList.FL_HIDDEN | FileHeader.FlagsList.FL_SYSTEM), 1, 1);
            writeFile("/", groupsHeader, Encoding.ASCII.GetBytes("1\nmain\n"));
            //Заголовок файла с учётными записями пользователей
            FileHeader usersHeader = new FileHeader("users", "sys", (byte)(FileHeader.FlagsList.FL_HIDDEN | FileHeader.FlagsList.FL_SYSTEM), 1, 1);
            writeFile("/", usersHeader, Encoding.ASCII.GetBytes("1\n" + adminLogin + "\n" + adminPassword + "\n1\n" + (int)UserInfo.Roles.ADMIN));
            //users.Data = "admin\nadmin\nadmin\nguest\n\nuser";
            /*FileHeader users2 = new FileHeader("users2", "sys", (byte)(FileHeader.FlagsList.FL_HIDDEN | FileHeader.FlagsList.FL_SYSTEM), 1, 1);
            string kbyte = "begin56789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCend";
            writeFile("/", users, Encoding.ASCII.GetBytes(kbyte + kbyte + kbyte + kbyte + kbyte + kbyte + kbyte + kbyte + kbyte + kbyte + kbyte + kbyte + kbyte + kbyte + kbyte + kbyte)); //16KB
            writeFile("/", users2, Encoding.ASCII.GetBytes(kbyte + kbyte + kbyte + kbyte + kbyte)); //5KB
            deleteFile(users);
            writeFile("/", users, Encoding.ASCII.GetBytes(kbyte + kbyte + kbyte + kbyte + kbyte + kbyte)); //6KB*/

            FileHeader kek1 = new FileHeader("kek1", "", (byte)(FileHeader.FlagsList.FL_DIRECTORY), 1, 1);
            writeFile("/", kek1, Encoding.ASCII.GetBytes(""));
            FileHeader kek2 = new FileHeader("kek2", "", (byte)(FileHeader.FlagsList.FL_DIRECTORY), 1, 1);
            writeFile("/kek1/", kek2, Encoding.ASCII.GetBytes(""));
            FileHeader kek3 = new FileHeader("kek3", "aza", 0, 1, 1);
            writeFile("/kek1/kek2/", kek3, Encoding.ASCII.GetBytes("Mama ama kek3.aza!"));
            Console.WriteLine(groupsHeader.FirstCluster);
            Console.WriteLine(usersHeader.FirstCluster);
            Console.WriteLine(kek1.FirstCluster);
            Console.WriteLine(kek2.FirstCluster);
            Console.WriteLine(kek3.FirstCluster);
        }

        public void openSpace(string path)
        {
            closeSpace();
            this.path = path;
            space = System.IO.File.Open(path, FileMode.Open, FileAccess.ReadWrite); //TODO 14.11: предупреждать, если файл не найден
            bw = new BinaryWriter(space);
            br = new BinaryReader(space);

            //Системная область
            br.BaseStream.Seek(0, SeekOrigin.Begin);
            this.SuperBlock = new SuperBlock(this, br.BaseStream);
            br.BaseStream.Seek(superBlock.Fat1Offset, SeekOrigin.Begin);
            Fat = new FAT(this, (int)superBlock.DiskSize / superBlock.ClusterSize, br.BaseStream);

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
        
        public int writeFile(string path, FileHeader fileHeader, byte[] data)
        {
            writeArea(Areas.FAT2);
            fileHeader.FirstCluster = fat.getFreeClusterIndex();
            fileHeader.Size = (uint)data.Length;
            if (data.Length == 0)
                data = Encoding.ASCII.GetBytes("\0");

            //Запись заголовка
            //TODO 14.11: тут использовать путь из path
            //TODO 15.11: проверить, что такого файла ещё нет
            if (path != null)
                while (path.Length > 0 && path[path.Length - 1] == UsefulThings.PATH_SEPARATOR)
                {
                    path = path.Remove(path.Length - 1);
                }
            if (path == null || path.Length == 0)
            {
                //TODO 15.11: проверить, есть ли ещё место в корневом каталоге
                rootDir = Encoding.ASCII.GetString(fileHeader.toByteArray(false)) + rootDir.Remove(superBlock.RootSize - FileHeader.SIZE, FileHeader.SIZE);
                writeArea(Areas.ROOTDIR);
            }
            else
            {
                string pathWithoutLastDir = path.Remove(path.LastIndexOf(UsefulThings.PATH_SEPARATOR));
                string lastDirName = path.Substring(path.LastIndexOf(UsefulThings.PATH_SEPARATOR) + 1);
                FileHeader lastDirHeader = getFileHeader(pathWithoutLastDir, lastDirName);
                if (lastDirHeader == null)
                    return 1;
                br.BaseStream.Seek(lastDirHeader.FirstCluster * superBlock.ClusterSize, SeekOrigin.Begin);
                //В directory записывается fileHeader (входной параметр), за которым следует содержимое директории, к которой относится lastDirHeader
                byte[] directory = fileHeader.toByteArray(false).Concat(br.ReadBytes((int)lastDirHeader.Size).AsEnumerable()).ToArray();

                /*int dirOldClSize = (int)Math.Ceiling((double)(directory.Length - FileHeader.SIZE) / superBlock.ClusterSize);
                int dirNewClSize = (int)Math.Ceiling((double)(directory.Length) / superBlock.ClusterSize);
                if (dirNewClSize > dirOldClSize)
                {
                    //Перезапись требует больше блоков
                    deleteFile(pathWithoutLastDir, lastDirHeader);
                    writeFile(pathWithoutLastDir, lastDirHeader, directory);
                }
                else
                {
                    //Перезаписать можно на старое место
                    lastDirHeader.Size += FileHeader.SIZE;
                    bw.Seek(lastDirHeader.FirstCluster * superBlock.ClusterSize, SeekOrigin.Begin);
                }*/
                deleteFile(pathWithoutLastDir, lastDirHeader);
                writeFile(pathWithoutLastDir, lastDirHeader, directory);
            }

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

        public void deleteFile(string path, FileHeader header)
        {
            int currCluster = header.FirstCluster;
            if (currCluster < superBlock.DataOffset / superBlock.ClusterSize)
                return; //Попытка обнулить системные блоки или блоки корневого каталога

            //TODO 15.11: удалять также запись в родительской директории
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

        public void restoreFat1()
        {
            //TODO
        }

        private FileHeader getFileHeader(string path, string filename, string extension = "")
        {
            //TODO SOMETHING?
            filename = UsefulThings.setStringLength(filename, FileHeader.NAME_MAX_LENGTH);
            extension = UsefulThings.setStringLength(extension, FileHeader.EXTENSION_MAX_LENGTH);

            uint currCluster = superBlock.RootOffset / superBlock.ClusterSize, currClusterOffset;
            int clustersPassed, currDirSize = superBlock.RootSize;
            string[] dirs = path.Split(UsefulThings.PATH_SEPARATOR.ToString().ToArray(), StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < dirs.Length; ++i)
                dirs[i] = UsefulThings.setStringLength(dirs[i], FileHeader.NAME_MAX_LENGTH);
            int nextDir = 0;
            FileHeader fh = new FileHeader();
            bool success = true; //становится false каждый раз, когда начинается проход по очередной директории; становится true, если во время прохода найден следующий заголовок
            
            //Цикл по директориям пути
            while (success && nextDir < dirs.Length)
            {
                success = false;
                clustersPassed = 0;
                //Цикл по блокам файла
                while (!success && currCluster != FAT.CL_EOF)
                {
                    currClusterOffset = currCluster * superBlock.ClusterSize;
                    br.BaseStream.Seek(currClusterOffset, SeekOrigin.Begin);
                    //Цикл по записям блока
                    do
                    {
                        fh.fromByteStream(br.BaseStream);
                        success = fh.Name.Equals(dirs[nextDir]) && (fh.Flags & (byte)FileHeader.FlagsList.FL_DIRECTORY) > 0;
                        //Пока не успех И позиция в пределах текущего блока И считано меньше байт, чем в файле директории
                    } while (!success && br.BaseStream.Position - currClusterOffset < superBlock.ClusterSize && clustersPassed * superBlock.ClusterSize + br.BaseStream.Position - currClusterOffset < currDirSize);
                    currCluster = fat.Table[currCluster];
                    ++clustersPassed;
                }
                currCluster = fh.FirstCluster;
                ++nextDir;
            }

            if (success)
            {
                //Добрались до нужной директории, ищем сам файл
                success = false;
                clustersPassed = 0;
                //Цикл по блокам файла
                while (!success && currCluster != FAT.CL_EOF)
                {
                    currClusterOffset = currCluster * superBlock.ClusterSize;
                    br.BaseStream.Seek(currClusterOffset, SeekOrigin.Begin);
                    //Цикл по записям блока
                    do
                    {
                        fh.fromByteStream(br.BaseStream);
                        success = fh.Name.Equals(filename) && fh.Extension.Equals(extension);//Успех ли, если найденный файл - каталог? Пока закомментировано - да. && (fh.Flags & (byte)FileHeader.FlagsList.FL_DIRECTORY) == 0;
                        //Пока не успех И позиция в пределах текущего блока И считано меньше байт, чем в файле директории
                    } while (!success && br.BaseStream.Position - currClusterOffset < superBlock.ClusterSize && clustersPassed * superBlock.ClusterSize + br.BaseStream.Position - currClusterOffset < currDirSize);
                    //} while (!success && br.BaseStream.Position - currClusterOffset < superBlock.ClusterSize);
                    currCluster = fat.Table[currCluster];
                    ++clustersPassed;
                }
            }

            if (success)
            {
                //Файл найден
                return fh;
            }
            else
            {
                //Файл не найден
                return null;
            }
        }
    }
}
