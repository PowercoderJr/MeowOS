using MeowOS.FileSystem.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MeowOS.FileSystem
{
    public class FileSystemController
    {
        private enum Areas { SUPERBLOCK, FAT1, FAT2, ROOTDIR, DATA }

        public const ushort FACTOR = 1024;

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
            set => currDir = value;
        }
        private uint currDirOffset;
        //TODO 28.11: /. и /..?
        public uint CurrDirCluster
        {
            get => currDirOffset;
            set => currDirOffset = value;
        }
        private FileHeader bufferFH;
        public FileHeader BufferFH => bufferFH;
        private byte[] bufferData;
        public byte[] BufferData => bufferData;
        private string bufferRestorePath;
        public string BufferRestorePath => bufferRestorePath;

        public FileSystemController()
        {
            CurrDir = "";

            bufferFH = null;
            bufferData = null;
            bufferRestorePath = null;
        }

        public void createSpace(string path, string adminLogin, string adminDigest)
        {
            closeSpace();
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
            writeFile("/", groupsHeader, UsefulThings.ENCODING.GetBytes(UserInfo.DEFAULT_GROUP), false);
            //Заголовок файла с учётными записями пользователей
            FileHeader usersHeader = new FileHeader("users", "sys", (byte)(FileHeader.FlagsList.FL_HIDDEN | FileHeader.FlagsList.FL_SYSTEM), 1, 1);
            writeFile("/", usersHeader, UsefulThings.ENCODING.GetBytes(
                adminLogin + UsefulThings.USERDATA_SEPARATOR +
                adminDigest + UsefulThings.USERDATA_SEPARATOR + 
                "1" + UsefulThings.USERDATA_SEPARATOR + 
                (int)UserInfo.Roles.ADMIN), false);

            CurrDirCluster = superBlock.RootOffset / superBlock.ClusterSize;
        }

        public void openSpace(string path)
        {
            closeSpace();
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
            rootDir = br.ReadBytes(superBlock.RootSize);
            CurrDirCluster = superBlock.RootOffset / superBlock.ClusterSize;
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
        /// <param name="checkRights">Следует ли проверять права доступа при выполнении операции</param>
        public void writeFile(string path, FileHeader fileHeader, byte[] data, bool checkRights)
        {
            path = UsefulThings.clearExcessSeparators(path);
            writeArea(Areas.FAT2);
            ushort firstCluster = fat.getFreeClusterIndex();
            fileHeader.FirstCluster = firstCluster;
            fat.Table[firstCluster] = FAT.CL_WRITING;
            fileHeader.Size = (uint)(data == null ? 0 : data.Length);
            if (data == null || data.Length == 0)
                data = UsefulThings.ENCODING.GetBytes("\0");

            //Запись заголовка
            writeHeader(path, fileHeader, checkRights);

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

        /// <summary>Дописывает заголовок файла в указанную директорию</summary>
        /// <param name="path">Путь к директории, куда следует дописать заголовок</param>
        /// <param name="fileHeader">Дописываемый заголовок файла</param>
        /// <param name="checkRights">Следует ли проверять права доступа при выполнении операции</param>
        public void writeHeader(string path, FileHeader fileHeader, bool checkRights)
        {
            string checkFilename = path + "/" + fileHeader.NamePlusExtensionWithoutZeros;
            if (getFileHeader(checkFilename, false) != null)
                throw new FileAlreadyExistException(checkFilename, fileHeader.IsDirectory);

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
                rootDir = rootDir.Take(posToWrite).Concat(fileHeader.toByteArray()).Concat(rootDir.Skip(posToWrite + FileHeader.SIZE)).ToArray();
                writeArea(Areas.ROOTDIR);
            }
            else
            {
                string pathWithoutLastDir, lastDirName;
                UsefulThings.detachLastFilename(path, out pathWithoutLastDir, out lastDirName);
                FileHeader lastDirHeader = getFileHeader(path, false);
                if (lastDirHeader == null)
                    throw new InvalidPathException(path);
                if (lastDirHeader.IsReadonly)
                    throw new FileIsReadonlyException(lastDirHeader.IsDirectory);
                if (checkRights && !(Session.userInfo == null || Session.userInfo.Role == UserInfo.Roles.ADMIN ||
                    (lastDirHeader.AccessRights & (ushort)FileHeader.RightsList.OW) > 0 ||
                    (lastDirHeader.AccessRights & (ushort)FileHeader.RightsList.GW) > 0 && lastDirHeader.Gid == Session.userInfo.Gid ||
                    (lastDirHeader.AccessRights & (ushort)FileHeader.RightsList.UW) > 0 && lastDirHeader.Uid == Session.userInfo.Uid))
                    throw new HaveNoRightsException(HaveNoRightsException.Rights.R_WRITE);
                br.BaseStream.Seek(lastDirHeader.FirstCluster * superBlock.ClusterSize, SeekOrigin.Begin);
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
                    directory = directory.Concat(fileHeader.toByteArray()).ToArray();
                else
                    directory = directory.Take(posToWrite).Concat(fileHeader.toByteArray()).Concat(directory.Skip(posToWrite + FileHeader.SIZE)).ToArray();
                
                rewriteFile(pathWithoutLastDir, lastDirHeader, directory, checkRights);
            }
        }

        /// <summary>Удаляет файл из указанной директории</summary>
        /// <param name="path">Путь к директории, где расположен файл</param>
        /// <param name="fileHeader">Заголовок файла</param>
        /// <param name="checkRights">Следует ли проверять права доступа при выполнении операции</param>
        /// <param name="isRecursive">Удалять ли вложенные файлы рекурсивно, если fileHeader указывает на каталог</param>
        public void deleteFile(string path, FileHeader fileHeader, bool checkRights, bool isRecursive = true)
        {
            path = UsefulThings.clearExcessSeparators(path);
            int currCluster = fileHeader.FirstCluster;
            if (currCluster < superBlock.DataOffset / superBlock.ClusterSize)
                throw new ForbiddenOperationException();
            if (fileHeader.IsReadonly)
                throw new FileIsReadonlyException(fileHeader.IsDirectory);
            if (checkRights && !(Session.userInfo == null || Session.userInfo.Role == UserInfo.Roles.ADMIN ||
                (fileHeader.AccessRights & (ushort)FileHeader.RightsList.OW) > 0 ||
                (fileHeader.AccessRights & (ushort)FileHeader.RightsList.GW) > 0 && fileHeader.Gid == Session.userInfo.Gid ||
                (fileHeader.AccessRights & (ushort)FileHeader.RightsList.UW) > 0 && fileHeader.Uid == Session.userInfo.Uid))
                throw new HaveNoRightsException(HaveNoRightsException.Rights.R_WRITE);

            if (!path.Equals(""))
            {
                FileHeader thisDirFH = getFileHeader(path, false);
                if (checkRights && !(Session.userInfo == null || Session.userInfo.Role == UserInfo.Roles.ADMIN ||
                    (thisDirFH.AccessRights & (ushort)FileHeader.RightsList.OW) > 0 ||
                    (thisDirFH.AccessRights & (ushort)FileHeader.RightsList.GW) > 0 && thisDirFH.Gid == Session.userInfo.Gid ||
                    (thisDirFH.AccessRights & (ushort)FileHeader.RightsList.UW) > 0 && thisDirFH.Uid == Session.userInfo.Uid))
                    throw new HaveNoRightsException(HaveNoRightsException.Rights.R_WRITE);
            }

            if (fileHeader.IsDirectory && isRecursive)
            {
                byte[] content = readFile(fileHeader, false);
                for (int offset = 0; offset < content.Length; offset += FileHeader.SIZE)
                {
                    byte[] curr = content.Skip(offset).ToArray();
                    deleteFile(path + "/" + fileHeader.NameWithoutZeros, new FileHeader(curr), true);
                }
            }

            //Удаление записи из родительской директории
            deleteHeader(path, fileHeader, checkRights);

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

        /// <summary>Удаляет заголовок файла из указанной директории</summary>
        /// <param name="path">Путь к директории, где записан заголовок</param>
        /// <param name="fileHeader">Заголовок файла</param>
        /// <param name="checkRights">Следует ли проверять права доступа при выполнении операции</param>
        public void deleteHeader(string path, FileHeader fileHeader, bool checkRights)
        {
            path = UsefulThings.clearExcessSeparators(path);
            string fullpath = path + '/' + (fileHeader.IsDirectory ? fileHeader.Name : (fileHeader.Name + '.' + fileHeader.Extension));
            int offset = (int)getFileHeaderOffset(fullpath, false);
            if (offset < 0)
                throw new InvalidPathException(fullpath);
            else if (offset >= superBlock.DataOffset)
            {
                br.BaseStream.Seek(offset, SeekOrigin.Begin);
                FileHeader fh = new FileHeader(br.BaseStream);
                if (fh.IsReadonly)
                    throw new FileIsReadonlyException(fh.IsDirectory);
                if (checkRights && !(Session.userInfo == null || Session.userInfo.Role == UserInfo.Roles.ADMIN ||
                    (fh.AccessRights & (ushort)FileHeader.RightsList.OW) > 0 ||
                    (fh.AccessRights & (ushort)FileHeader.RightsList.GW) > 0 && fh.Gid == Session.userInfo.Gid ||
                    (fh.AccessRights & (ushort)FileHeader.RightsList.UW) > 0 && fh.Uid == Session.userInfo.Uid))
                    throw new HaveNoRightsException(HaveNoRightsException.Rights.R_WRITE);
            }

            bw.Seek(offset, SeekOrigin.Begin);
            bw.Write(UsefulThings.DELETED_MARK);
            if (offset >= superBlock.RootOffset && offset < superBlock.DataOffset)
                rootDir[offset - superBlock.RootOffset] = (byte)UsefulThings.DELETED_MARK;
        }

        /// <summary>Перезаписывает файл по указанному пути</summary>
        /// <param name="path">Путь к директории, куда следует записать файл</param>
        /// <param name="fileHeader">Заголовок файла</param>
        /// <param name="data">Содержимое файла</param>
        /// <param name="checkRights">Следует ли проверять права доступа при выполнении операции</param>
        public void rewriteFile(string path, FileHeader fileHeader, byte[] data, bool checkRights)
        {
            byte[] buf = null;
            try
            {
                if (getFileHeader(path + "/" + fileHeader.NamePlusExtensionWithoutZeros, false) != null)
                {
                    buf = readFile(fileHeader, false);
                    deleteFile(path, fileHeader, checkRights, false);
                }
                writeFile(path, fileHeader, data, checkRights);
            }
            catch (Exception e)
            {
                if (!(e is HaveNoRightsException))
                {
                    deleteFile(path, fileHeader, false);
                    if (buf != null)
                        writeFile(path, fileHeader, buf, false);
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

        /// <summary>Возвращает заголовок файла, расположенного по заданному смещению на диске</summary>
        /// <param name="offset">Абсолютное смещение</param>
        /// <returns>Заголовок файла</returns>
        private FileHeader getFileHeader(long offset)
        {
            if (offset == -1)
                return null;

            br.BaseStream.Seek(offset, SeekOrigin.Begin);
            return new FileHeader(br.BaseStream);
        }

        /// <summary>Возвращает заголовок файла, расположенного по указанному пути</summary>
        /// <param name="path">Полный путь к файлу</param>
        /// <param name="checkRights">Следует ли проверять права доступа при выполнении операции</param>
        /// <returns>Заголовок файла</returns>
        public FileHeader getFileHeader(string path, bool checkRights)
        {
            return getFileHeader(getFileHeaderOffset(path, checkRights));
        }

        /// <summary>Возвращает заголовок файла с заданным именем в директории по заданному смещению</summary>
        /// <param name="filename">Имя файла с расширением (либо без, если это каталог)</param>
        /// <param name="dirFirstCluster">Начальный блок директории, в которой будет осуществлён поиск</param>
        /// <param name="checkRights">Следует ли проверять права доступа при выполнении операции</param>
        /// <returns>Заголовок файла</returns>
        public FileHeader getFileHeader(string filename, uint dirFirstCluster, bool checkRights)
        {
            return getFileHeader(getFileHeaderOffset(filename, dirFirstCluster, checkRights));
        }

        /// <summary>Возвращает абсолютное смещение заголовка файла, расположенного по указанному пути</summary>
        /// <param name="path">Полный путь к файлу</param>
        /// <returns>Смещение заголовка файла</returns>
        public long getFileHeaderOffset(string path, bool checkRights)
        {
            uint currCluster = superBlock.RootOffset / superBlock.ClusterSize;
            string[] steps = path.Split(UsefulThings.PATH_SEPARATOR.ToString().ToArray(), StringSplitOptions.RemoveEmptyEntries);
            int nextStep = 0;
            int nextHeaderOffset = steps.Length > 0 ? 0 : -1;

            //Цикл по директориям пути
            while (nextHeaderOffset >= 0 && nextStep < steps.Length)
            {
                nextHeaderOffset = (int)getFileHeaderOffset(steps[nextStep], currCluster, checkRights);
                if (nextHeaderOffset >= 0)
                {
                    br.BaseStream.Seek(nextHeaderOffset, SeekOrigin.Begin);
                    FileHeader fh = new FileHeader(br.BaseStream);
                    currCluster = fh.FirstCluster;
                    ++nextStep;
                }
            }            
            return nextStep == steps.Length ? nextHeaderOffset : -1;
        }

        /// <summary>Возвращает абсолютное смещение заголовка файла с заданным именем в директории по заданному смещению</summary>
        /// <param name="filename">Имя файла с расширением (либо без, если это каталог)</param>
        /// <param name="dirFirstCluster">Начальный блок директории, в которой будет осуществлён поиск</param>
        /// <param name="checkRights">Следует ли проверять права доступа при выполнении операции</param>
        /// <returns>Смещение заголовка файла</returns>
        public long getFileHeaderOffset(string filename, uint dirFirstCluster, bool checkRights)
        {
            string extension;
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
            
            bool stillWithinCluster = false, stillHeadersInFile = false, success = false;
            uint currCluster = dirFirstCluster;
            uint headersRead = 0;
            FileHeader fh = null;
            //Цикл по блокам файла
            while (!success && currCluster != FAT.CL_EOF)
            {
                uint currClusterOffset = currCluster * superBlock.ClusterSize;
                br.BaseStream.Seek(currClusterOffset, SeekOrigin.Begin);
                //Цикл по записям блока
                headersRead = 0;
                do
                {
                    fh = new FileHeader(br.BaseStream);                    
                    success = fh.Name.Equals(filename) && fh.Extension.Equals(extension);
                    stillWithinCluster = br.BaseStream.Position - currClusterOffset < superBlock.ClusterSize;
                    stillHeadersInFile = fh.Name[0] != '\0';
                    ++headersRead;
                } while (!success && stillWithinCluster && stillHeadersInFile);
                if (!success)
                    currCluster = fat.Table[currCluster];
            }

            if (success)
            {
                if (checkRights && !(Session.userInfo == null || Session.userInfo.Role == UserInfo.Roles.ADMIN ||
                    (fh.AccessRights & (ushort)FileHeader.RightsList.OR) > 0 ||
                    (fh.AccessRights & (ushort)FileHeader.RightsList.GR) > 0 && fh.Gid == Session.userInfo.Gid ||
                    (fh.AccessRights & (ushort)FileHeader.RightsList.UR) > 0 && fh.Uid == Session.userInfo.Uid))
                    throw new HaveNoRightsException(HaveNoRightsException.Rights.R_READ);
                return currCluster * superBlock.ClusterSize + (headersRead - 1) * FileHeader.SIZE;
            }
            else
                return -1;
        }

        /// <summary>Возвращает содержимое файла, расположенного по указанному пути</summary>
        /// <param name="path">Полный путь к файлу</param>
        /// <param name="checkRights">Следует ли проверять права доступа при выполнении операции</param>
        /// <returns>Содержимое файла</returns>
        public byte[] readFile(string path, bool checkRights)
        {
            path = UsefulThings.clearExcessSeparators(path);

            if (path.Equals(""))
                return rootDir.Take(getRootdirFactSize()).ToArray();

            FileHeader fh = getFileHeader(path, checkRights);
            if (fh == null)
                throw new InvalidPathException(path);
            return readFile(fh, checkRights);
        }

        /// <summary>Возвращает содержимое файла, к которому относится указанный заголовок</summary>
        /// <param name="fh">Заголовок файла</param>
        /// <param name="checkRights">Следует ли проверять права доступа при выполнении операции</param>
        /// <returns>Содержимое файла</returns>
        public byte[] readFile(FileHeader fh, bool checkRights)
        {
            if (checkRights && !(Session.userInfo == null || Session.userInfo.Role == UserInfo.Roles.ADMIN ||
                (fh.AccessRights & (ushort)FileHeader.RightsList.OR) > 0 ||
                (fh.AccessRights & (ushort)FileHeader.RightsList.GR) > 0 && fh.Gid == Session.userInfo.Gid ||
                (fh.AccessRights & (ushort)FileHeader.RightsList.UR) > 0 && fh.Uid == Session.userInfo.Uid))
                throw new HaveNoRightsException(HaveNoRightsException.Rights.R_READ);

            byte[] result = new byte[fh.Size];
            int currCluster = fh.FirstCluster, toRead = (int)fh.Size, toReadOnThisStep;
            while (currCluster != FAT.CL_EOF && toRead > 0)
            {
                toReadOnThisStep = Math.Min(superBlock.ClusterSize, toRead);
                br.BaseStream.Seek(currCluster * superBlock.ClusterSize, SeekOrigin.Begin);
                Buffer.BlockCopy(br.ReadBytes(toReadOnThisStep), 0, result, (int)fh.Size - toRead, toReadOnThisStep);
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

        public void writeToBuffer(FileHeader fh, byte[] data, string restorePath)
        {
            bufferFH = fh == null ? null : fh.Clone() as FileHeader;
            bufferData = data;
            bufferRestorePath = restorePath;
        }

        public void writeFromBuffer(string path)
        {
            if (bufferFH.IsDirectory)
            {
                writeFile(path, bufferFH, null, true);
                byte[] data = bufferData.ToArray();
                for (int offset = 0; offset < data.Length; offset += FileHeader.SIZE)
                {
                    string newPath = path + "/" + bufferFH.NameWithoutZeros;
                    FileHeader oldBufferFH = bufferFH;
                    byte[] oldBufferData = bufferData;
                    bufferFH = new FileHeader(data.Skip(offset).ToArray());
                    bufferData = readFile(bufferFH, true);
                    writeFromBuffer(newPath);
                    bufferFH = oldBufferFH;
                    bufferData = oldBufferData;
                }
            }
            else
                writeFile(path, bufferFH, bufferData, true);
        }

        public void clearBuffer()
        {
            bufferFH = null;
            bufferData = null;
            bufferRestorePath = null;
        }

        ~FileSystemController()
        {
            closeSpace();
        }
    }
}
