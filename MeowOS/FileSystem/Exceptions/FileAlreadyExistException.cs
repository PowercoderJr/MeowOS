using System;

namespace MeowOS.FileSystem.Exceptions
{
    class FileAlreadyExistException : Exception
    {
        public FileAlreadyExistException(string filename, bool isDirectory) : base((isDirectory ? "Директория" : "Файл") + " " + filename + " уже существует.")
        {
            
        }
    }
}
