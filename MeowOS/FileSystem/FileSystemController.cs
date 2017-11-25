using MeowOS.FileSystem;
using MeowOS.FileSystem.Exceptions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowOS.FileSystem
{
    public class FileSystemController
    {
        private enum Areas { SUPERBLOCK, FAT1, FAT2, ROOTDIR, DATA }

        public const ushort FACTOR = 1024;

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


        private string currDir;
        public string CurrDir
        {
            get => currDir;
            set => currDir = value.Length > 0 ? value : "/";
        }

        public FileSystemController()
        {
            CurrDir = "";
        }

        public void createSpace(string path, string adminLogin, string adminDigest)
        {
            closeSpace();
            this.path = path;
            space = File.Open(path, FileMode.Create, FileAccess.ReadWrite);
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
            writeFile("/", groupsHeader, UsefulThings.ENCODING.GetBytes(UserInfo.DEFAULT_GROUP));
            //Заголовок файла с учётными записями пользователей
            FileHeader usersHeader = new FileHeader("users", "sys", (byte)(FileHeader.FlagsList.FL_HIDDEN | FileHeader.FlagsList.FL_SYSTEM), 1, 1);
            writeFile("/", usersHeader, UsefulThings.ENCODING.GetBytes(
                adminLogin + UsefulThings.USERDATA_SEPARATOR +
                adminDigest + UsefulThings.USERDATA_SEPARATOR + 
                "1" + UsefulThings.USERDATA_SEPARATOR + 
                (int)UserInfo.Roles.ADMIN));

            /*FileHeader justFile = new FileHeader("justFile", "txt", 0, 1, 1);
            writeFile("/", justFile, null);
            FileHeader kek1 = new FileHeader("kek1", "", (byte)(FileHeader.FlagsList.FL_DIRECTORY), 1, 1);
            writeFile("/", kek1, null);
            FileHeader kek1Hidden = new FileHeader("kek1hidden", "", (byte)(FileHeader.FlagsList.FL_DIRECTORY | FileHeader.FlagsList.FL_HIDDEN), 1, 1);
            writeFile("/", kek1Hidden, null);
            FileHeader kek2 = new FileHeader("kek2", "", (byte)(FileHeader.FlagsList.FL_DIRECTORY), 1, 1);
            writeFile("/kek1/", kek2, null);
            FileHeader kek3 = new FileHeader("kek3", "aza", 0, 1, 1);
            writeFile("/kek1/kek2/", kek3, UsefulThings.ENCODING.GetBytes("Mama ama kek3.aza!"));
            FileHeader kek4 = new FileHeader("kek4", "aza", 0, 1, 1);
            writeFile("/kek1/kek2/", kek4, UsefulThings.ENCODING.GetBytes("Mama ama kek4.aza!"));
            deleteFile("/kek1/kek2/", kek3);*/
        }

        public void openSpace(string path)
        {
            closeSpace();
            this.path = path;
            space = File.Open(path, FileMode.Open, FileAccess.ReadWrite);
            bw = new BinaryWriter(space);
            br = new BinaryReader(space);

            //Системная область
            br.BaseStream.Seek(0, SeekOrigin.Begin);
            this.SuperBlock = new SuperBlock(this, br.BaseStream);
            br.BaseStream.Seek(superBlock.Fat1Offset, SeekOrigin.Begin);
            Fat = new FAT(this, (int)superBlock.DiskSize / superBlock.ClusterSize, br.BaseStream);

            //Корневой каталог
            br.BaseStream.Seek((int)superBlock.RootOffset, SeekOrigin.Begin);
            //rootDir = UsefulThings.ENCODING.GetString(br.ReadBytes(superBlock.RootSize));
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

        /// <summary>Записывает файл в указанную директорию</summary>
        /// <param name="path">Путь к директории, куда следует записать файл</param>
        /// <param name="fileHeader">Заголовок файла</param>
        /// <param name="data">Содержимое файла</param>
        public void writeFile(string path, FileHeader fileHeader, byte[] data)
        {
            path = UsefulThings.clearExcessSeparators(path);
            string checkFilename = path + "/" + fileHeader.NamePlusExtension;
            if (getFileHeader(checkFilename) != null)
                throw new FileAlreadyExistException(checkFilename, fileHeader.IsDirectory);

            writeArea(Areas.FAT2);
            ushort firstCluster = fat.getFreeClusterIndex();
            fileHeader.FirstCluster = firstCluster;
            fat.Table[firstCluster] = FAT.CL_WRITING;
            fileHeader.Size = (uint)(data == null ? 0 : data.Length);
            if (data == null || data.Length == 0)
                data = UsefulThings.ENCODING.GetBytes("\0");

            //Запись заголовка
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
                    throw new RootdirOutOfSpaceException();
                rootDir = rootDir.Take(posToWrite).Concat(fileHeader.toByteArray(false)).Concat(rootDir.Skip(posToWrite + FileHeader.SIZE)).ToArray();
                writeArea(Areas.ROOTDIR);
            }
            else
            {
                string pathWithoutLastDir, lastDirName;
                UsefulThings.detachLastFilename(path, out pathWithoutLastDir, out lastDirName);
                FileHeader lastDirHeader = getFileHeader(path);
                if (lastDirHeader == null)
                    throw new InvalidPathException();
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
                
                //deleteFile(pathWithoutLastDir, lastDirHeader);
                rewriteFile(pathWithoutLastDir, lastDirHeader, directory);
            }

            //Запись данных
            int toWrite = data.Length, toWriteOnThisStep, i = 0;
            List<ushort> usedClusters = new List<ushort>();

            ushort currCluster = firstCluster;
            while (toWrite > 0)
            {
                if (currCluster < fat.TableSize)
                {
                    usedClusters.Add(currCluster);
                    fat.Table[currCluster] = FAT.CL_WRITING;
                    toWriteOnThisStep = Math.Min(superBlock.ClusterSize, toWrite);
                    bw.Seek(currCluster * superBlock.ClusterSize, SeekOrigin.Begin);
                    bw.Write(data, superBlock.ClusterSize * i++, toWriteOnThisStep);
                    toWrite -= toWriteOnThisStep;
                    currCluster = fat.getFreeClusterIndex();
                }
                else
                {
                    for (i = 0; i < usedClusters.Count; ++i)
                        fat.Table[usedClusters[i]] = FAT.CL_FREE;
                    throw new DiskOutOfSpaceException();
                }
            }
            for (i = 0; i < usedClusters.Count - 1; ++i)
                fat.Table[usedClusters[i]] = usedClusters[i + 1];
            fat.Table[usedClusters.Last()] = FAT.CL_EOF;
            writeArea(Areas.FAT1);
        }

        /// <summary>Удаляет файл из указанной директории</summary>
        /// <param name="path">Путь к директории, где расположен файл</param>
        /// <param name="fileHeader">Заголовок файла</param>
        /// <param name="deleteHeader">Нужно ли удалять заголовок из директории</param>
        public void deleteFile(string path, FileHeader fileHeader, bool deleteHeader = true)
        {
            //TODO 21.11: рекурсивное удаление, если удаляется директория
            int currCluster = fileHeader.FirstCluster;
            if (currCluster < superBlock.DataOffset / superBlock.ClusterSize)
                throw new ForbiddenOperationException();

            if (deleteHeader)
            {
                //Удаление записи из родительской директории
                path = UsefulThings.clearExcessSeparators(path);
                //string pathWithoutLast, last;
                //UsefulThings.detachLastFilename(path, out pathWithoutLast, out last);
                FileHeader fh = new FileHeader();
                string fullpath = path + '/' + (fileHeader.IsDirectory ? fileHeader.Name : (fileHeader.Name + '.' + fileHeader.Extension));
                int offset = (int)getFileHeaderOffset(fullpath);
                if (offset < 0)
                    throw new InvalidPathException();
                bw.Seek(offset, SeekOrigin.Begin);
                bw.Write(UsefulThings.DELETED_MARK);
                if (offset >= superBlock.RootOffset && offset < superBlock.DataOffset)
                    rootDir[offset - superBlock.RootOffset] = (byte)UsefulThings.DELETED_MARK;
            }

            //Помечание блоков как свободных
            writeArea(Areas.FAT2);
            int nextCluster;
            while (fat.Table[currCluster] != FAT.CL_EOF && fat.Table[currCluster] != FAT.CL_FREE)
            {
                nextCluster = fat.Table[currCluster];
                fat.Table[currCluster] = FAT.CL_FREE;
                currCluster = nextCluster;
            }
            fat.Table[currCluster] = FAT.CL_FREE;
            writeArea(Areas.FAT1);
        }
        
        /// <summary>Перезаписывает файл по указанному пути</summary>
        /// <param name="path">Путь к директории, куда следует записать файл</param>
        /// <param name="fileHeader">Заголовок файла</param>
        /// <param name="data">Содержимое файла</param>
        public void rewriteFile(string path, FileHeader fileHeader, byte[] data)
        {
            byte[] buf = null;
            if (getFileHeader(path + fileHeader.NamePlusExtension) != null)
            {
                buf = readFile(fileHeader);
                deleteFile(path, fileHeader);
            }
            try
            {
                writeFile(path, fileHeader, data);
            }
            catch (Exception e)
            {
                deleteFile(path, fileHeader);
                if (buf != null)
                {
                    writeFile(path, fileHeader, buf);
                }
                throw e;
            }

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

        public void writeBytes(int offset, byte[] data)
        {
            if (offset >= superBlock.RootOffset && offset < superBlock.DataOffset)
            {
                int offsetInRootDir = (int)(offset - superBlock.RootOffset);
                if (offsetInRootDir + data.Length > superBlock.RootSize)
                    throw new RootdirOutOfSpaceException();
                //rootDir[offset - superBlock.RootOffset] = (byte)UsefulThings.DELETED_MARK;
                rootDir = rootDir.Take(offsetInRootDir).Concat(data).Concat(rootDir.Skip(offsetInRootDir + data.Length)).ToArray();
            }
            else if (offset + data.Length > superBlock.DiskSize)
                throw new DiskOutOfSpaceException();

            bw.Seek(offset, SeekOrigin.Begin);
            bw.Write(data);
        }

        public void restoreFat()
        {
            br.BaseStream.Seek(superBlock.Fat2Offset, SeekOrigin.Begin);
            fat.fromByteStream(br.BaseStream);
            writeArea(Areas.FAT1);
        }

        /// <summary>Возвращает заголовок файла, расположенного по указанному пути</summary>
        /// <param name="path">Полный путь к файлу</param>
        /// <returns>Заголовок файла</returns>
        public FileHeader getFileHeader(string path)
        {
            long offset = getFileHeaderOffset(path);
            if (offset == -1)
                return null;

            br.BaseStream.Seek(offset, SeekOrigin.Begin);
            FileHeader fh = new FileHeader();
            fh.fromByteStream(br.BaseStream);
            return fh;
        }

        /// <summary>Возвращает смещение заголовка файла, расположенного по указанному пути</summary>
        /// <param name="path">Полный путь к файлу</param>
        /// <returns>Смещение заголовка файла</returns>
        public long getFileHeaderOffset(string path)
        {
            if (UsefulThings.clearExcessSeparators(path).Equals(""))
                return superBlock.RootOffset;

            string filename, extension;
            UsefulThings.detachLastFilename(path, out path, out filename);
            int indexOfDot = filename.IndexOf('.');
            if (indexOfDot < 0)
            {
                extension = "";
            }
            else
            {
                extension = filename.Substring(indexOfDot + 1);
                filename = filename.Substring(0, indexOfDot);
            }
            if (filename.Length > FileHeader.NAME_MAX_LENGTH || extension.Length > FileHeader.EXTENSION_MAX_LENGTH)
                return -1; //Таких файлов не может существовать

            filename = UsefulThings.setStringLength(filename, FileHeader.NAME_MAX_LENGTH);
            extension = UsefulThings.setStringLength(extension, FileHeader.EXTENSION_MAX_LENGTH);

            uint currCluster = superBlock.RootOffset / superBlock.ClusterSize, currClusterOffset;
            int currDirSize = superBlock.RootSize;
            string[] dirs = path.Split(UsefulThings.PATH_SEPARATOR.ToString().ToArray(), StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < dirs.Length; ++i)
                dirs[i] = UsefulThings.setStringLength(dirs[i], FileHeader.NAME_MAX_LENGTH);
            int nextDir = 0;
            FileHeader fh = new FileHeader();
            bool success = true; //становится false каждый раз, когда начинается проход по очередной директории; становится true, если во время прохода найден следующий заголовок
            bool stillWithinCluster = false, stillHeadersInFile = false;

            //Цикл по директориям пути
            while (success && nextDir < dirs.Length)
            {
                success = false;
                //Цикл по блокам файла
                while (!success && currCluster != FAT.CL_EOF)
                {
                    currClusterOffset = currCluster * superBlock.ClusterSize;
                    br.BaseStream.Seek(currClusterOffset, SeekOrigin.Begin);
                    //Цикл по записям блока
                    do
                    {
                        fh.fromByteStream(br.BaseStream);
                        success = fh.Name.Equals(dirs[nextDir]) && fh.IsDirectory;
                        stillWithinCluster = br.BaseStream.Position - currClusterOffset < superBlock.ClusterSize;
                        stillHeadersInFile = fh.Name[0] != '\0';
                    } while (!success && stillWithinCluster && stillHeadersInFile);
                    currCluster = fat.Table[currCluster];
                }
                currCluster = fh.FirstCluster;
                ++nextDir;
            }

            if (success)
            {
                //Добрались до нужной директории, ищем сам файл
                success = false;
                //Цикл по блокам файла
                while (!success && currCluster != FAT.CL_EOF)
                {
                    currClusterOffset = currCluster * superBlock.ClusterSize;
                    br.BaseStream.Seek(currClusterOffset, SeekOrigin.Begin);
                    //Цикл по записям блока
                    do
                    {
                        fh.fromByteStream(br.BaseStream);
                        success = fh.Name.Equals(filename) && fh.Extension.Equals(extension); //Успех ли, если найденный файл - каталог? Пока закомментировано - да. && !fh.isDirectory;
                        stillWithinCluster = br.BaseStream.Position - currClusterOffset < superBlock.ClusterSize;
                        stillHeadersInFile = fh.Name[0] != '\0';
                    } while (!success && stillWithinCluster && stillHeadersInFile);
                    currCluster = fat.Table[currCluster];
                }
            }

            if (success && stillWithinCluster && stillHeadersInFile)
                //Файл найден
                return br.BaseStream.Position - FileHeader.SIZE;
            else
                //Файл не найден
                return -1;
        }

        /// <summary>Возвращает содержимое файла, расположенного по указанному пути</summary>
        /// <param name="path">Полный путь к файлу</param>
        /// <returns>Содержимое файла</returns>
        public byte[] readFile(string path)
        {
            if (UsefulThings.clearExcessSeparators(path).Length == 0)
                return rootDir.Take(getRootdirFactSize()).ToArray();
            else
            {
                FileHeader fh = getFileHeader(path);
                if (fh == null)
                    throw new InvalidPathException();
                return readFile(fh);
            }
        }

        /// <summary>Возвращает содержимое файла, к которому относится указанный заголовок</summary>
        /// <param name="fh">Заголовок файла</param>
        /// <returns>Содержимое файла</returns>
        public byte[] readFile(FileHeader fh)
        {
            byte[] result = new byte[0];
            int currCluster = fh.FirstCluster, toRead = (int)fh.Size, toReadOnThisStep;
            while (currCluster != FAT.CL_EOF && toRead > 0)
            {
                toReadOnThisStep = Math.Min(superBlock.ClusterSize, toRead);
                br.BaseStream.Seek(currCluster * superBlock.ClusterSize, SeekOrigin.Begin);
                result = result.Concat(br.ReadBytes(toReadOnThisStep)).ToArray();
                currCluster = fat.Table[currCluster];
                toRead -= toReadOnThisStep;
            }
            return result;
        }

        private int getRootdirFactSize()
        {
            int size = 0;
            while (rootDir[size] != '\0')
                size += FileHeader.SIZE;
            return size;
        }

        ~FileSystemController()
        {
            closeSpace();
        }
    }
}
