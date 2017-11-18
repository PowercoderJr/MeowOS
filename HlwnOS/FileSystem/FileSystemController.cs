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
    public class FileSystemController
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
        private byte[] rootDir;
        public byte[] RootDir
        {
            get { return rootDir; }
            set { rootDir = value; }
        }

        public FileSystemController()
        {
            ;
        }

        public void createSpace(string path, string adminLogin, string adminDigest)
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
            writeFile("/", groupsHeader, Encoding.ASCII.GetBytes(UserInfo.DEFAULT_GROUP + UsefulThings.EOLN_STR));
            //Заголовок файла с учётными записями пользователей
            FileHeader usersHeader = new FileHeader("users", "sys", (byte)(FileHeader.FlagsList.FL_HIDDEN | FileHeader.FlagsList.FL_SYSTEM), 1, 1);
            writeFile("/", usersHeader, Encoding.ASCII.GetBytes(
                adminLogin + UsefulThings.USERDATA_SEPARATOR +
                adminDigest + UsefulThings.USERDATA_SEPARATOR + 
                "1" + UsefulThings.USERDATA_SEPARATOR + 
                (int)UserInfo.Roles.ADMIN + UsefulThings.EOLN_STR));

            FileHeader kek1 = new FileHeader("kek1", "", (byte)(FileHeader.FlagsList.FL_DIRECTORY), 1, 1);
            writeFile("/", kek1, Encoding.ASCII.GetBytes(""));
            FileHeader kek2 = new FileHeader("kek2", "", (byte)(FileHeader.FlagsList.FL_DIRECTORY), 1, 1);
            writeFile("/kek1/", kek2, Encoding.ASCII.GetBytes(""));
            FileHeader kek3 = new FileHeader("kek3", "aza", 0, 1, 1);
            writeFile("/kek1/kek2/", kek3, Encoding.ASCII.GetBytes("Mama ama kek3.aza!"));
            FileHeader kek4 = new FileHeader("kek4", "aza", 0, 1, 1);
            writeFile("/kek1/kek2/", kek4, Encoding.ASCII.GetBytes("Mama ama kek4.aza!"));
            deleteFile("/kek1/kek2/", kek3);
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
            //rootDir = Encoding.ASCII.GetString(br.ReadBytes(superBlock.RootSize));
            rootDir = br.ReadBytes(superBlock.RootSize);
        }

        public void closeSpace()
        {
            if (space != null)
            {
                //space.Flush();
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
            //TODO 15.11: проверить, что такого файла ещё нет
            path = UsefulThings.clearExcessSeparators(path);
            int posToWrite;
            if (path == null || path.Length == 0)
            {
                posToWrite = 0;
                while (posToWrite + FileHeader.SIZE <= superBlock.RootSize)
                {
                    if (rootDir[posToWrite] == '\0' || rootDir[posToWrite] == UsefulThings.DELETED_MARK)
                        break;
                    else
                        posToWrite += FileHeader.SIZE;
                }
                if (posToWrite + FileHeader.SIZE > superBlock.RootSize)
                    return 2; //Нет места в корневом каталоге
                rootDir = rootDir.Take(posToWrite).Concat(fileHeader.toByteArray(false)).Concat(rootDir.Skip(posToWrite + FileHeader.SIZE)).ToArray();
                writeArea(Areas.ROOTDIR);
            }
            else
            {
                string pathWithoutLastDir, lastDirName;
                UsefulThings.detachLastFilename(path, out pathWithoutLastDir, out lastDirName);
                FileHeader lastDirHeader = getFileHeader(pathWithoutLastDir, lastDirName);
                if (lastDirHeader == null)
                    return 1;
                br.BaseStream.Seek(lastDirHeader.FirstCluster * superBlock.ClusterSize, SeekOrigin.Begin);
                //В directory записывается fileHeader (входной параметр), за которым следует содержимое директории, к которой относится lastDirHeader
                byte[] directory = br.ReadBytes((int)lastDirHeader.Size);
                posToWrite = 0;
                while (posToWrite + FileHeader.SIZE <= directory.Length)
                {
                    if (directory[posToWrite] == '\0' || directory[posToWrite] == UsefulThings.DELETED_MARK)
                        break;
                    else
                        posToWrite += FileHeader.SIZE;
                }
                if (posToWrite + FileHeader.SIZE > superBlock.RootSize)
                    directory = directory.Concat(fileHeader.toByteArray(false)).ToArray();
                else
                    directory = directory.Take(posToWrite).Concat(fileHeader.toByteArray(false)).Concat(directory.Skip(posToWrite + FileHeader.SIZE)).ToArray();

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

        public int deleteFile(string path, FileHeader fileHeader, bool deleteHeader = true)
        {
            int currCluster = fileHeader.FirstCluster;
            if (currCluster < superBlock.DataOffset / superBlock.ClusterSize)
                return 1; //Попытка обнулить системные блоки или блоки корневого каталога

            if (deleteHeader)
            {
                //Удаление записи из родительской директории
                path = UsefulThings.clearExcessSeparators(path);
                string pathWithoutLast, last;
                UsefulThings.detachLastFilename(path, out pathWithoutLast, out last);
                FileHeader fh = new FileHeader();
                int offset;
                bool success = false;
                if (path.Length == 0)
                {
                    offset = 0;
                    do
                    {
                        fh.fromByteArray(rootDir.Skip(offset).ToArray());
                        offset += FileHeader.SIZE;
                        success = fh.Name.Equals(fileHeader.Name) && fh.Extension.Equals(fileHeader.Extension);
                    } while (!success && offset < superBlock.RootSize && fh.Name[0] != '\0');
                    if (!success)
                        return 2; //Файл не найден
                    rootDir[offset - FileHeader.SIZE] = (byte)UsefulThings.DELETED_MARK;
                    writeArea(Areas.ROOTDIR);
                }
                else
                {
                    FileHeader parentDir = getFileHeader(pathWithoutLast, last);
                    if (parentDir == null)
                        return 2; //Файл не найден
                    int dirClustersPassed = 0, currDirCluster = parentDir.FirstCluster, currDirClusterOffset;
                    //Цикл по блокам файла
                    while (!success && currDirCluster != FAT.CL_EOF)
                    {
                        currDirClusterOffset = currDirCluster * superBlock.ClusterSize;
                        br.BaseStream.Seek(currDirClusterOffset, SeekOrigin.Begin);
                        //Цикл по записям блока
                        do
                        {
                            fh.fromByteStream(br.BaseStream);
                            success = fh.Name.Equals(fileHeader.Name) && fh.Extension.Equals(fileHeader.Extension);
                            //Пока не успех И позиция в пределах текущего блока И считано меньше байт, чем в файле директории
                        } while (!success && br.BaseStream.Position - currDirClusterOffset < superBlock.ClusterSize && dirClustersPassed * superBlock.ClusterSize + br.BaseStream.Position - currDirClusterOffset < parentDir.Size);
                        currDirCluster = fat.Table[currDirCluster];
                        ++dirClustersPassed;
                    }
                    if (!success)
                        return 2; //Файл не найден
                    bw.Seek(-FileHeader.SIZE, SeekOrigin.Current);
                    bw.Write(UsefulThings.DELETED_MARK);
                }
            }

            //Помечание блоков как свободных
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
            return 0;
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
                    buffer = rootDir;
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

        public void restoreFat()
        {
            br.BaseStream.Seek(superBlock.Fat2Offset, SeekOrigin.Begin);
            fat.fromByteStream(br.BaseStream);
            writeArea(Areas.FAT1);
        }

        public FileHeader getFileHeader(string path, string filename, string extension = "")
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
            bool stillWithinCluster, stillHeadersInFile; 
            
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
                        stillWithinCluster = br.BaseStream.Position - currClusterOffset < superBlock.ClusterSize;
                        stillHeadersInFile = fh.Name[0] != '\0';
                    } while (!success && stillWithinCluster && stillHeadersInFile);
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
                        success = fh.Name.Equals(filename) && fh.Extension.Equals(extension); //Успех ли, если найденный файл - каталог? Пока закомментировано - да. && (fh.Flags & (byte)FileHeader.FlagsList.FL_DIRECTORY) == 0;
                        stillWithinCluster = br.BaseStream.Position - currClusterOffset < superBlock.ClusterSize;
                        stillHeadersInFile = fh.Name[0] != '\0';
                    } while (!success && stillWithinCluster && stillHeadersInFile);
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

        public byte[] readFile(string path, string filename, string extension = "")
        {
            return readFile(getFileHeader(path, filename, extension));
        }

        public byte[] readFile(FileHeader fh)
        {
            //TODO 17.11: читать файлы, разбитые на блоки
            br.BaseStream.Seek(fh.FirstCluster * superBlock.ClusterSize, SeekOrigin.Begin);
            return br.ReadBytes((int)fh.Size);
        }

        ~FileSystemController()
        {
            closeSpace();
        }
    }
}
