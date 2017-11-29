using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowOS.FileSystem.Exceptions
{
    class FileAlreadyExistException : Exception
    {
        public FileAlreadyExistException(string filename, bool isDirectory) : base((isDirectory ? "Директория" : "Файл") + " " + filename + " уже существует.")
        {
            
        }
    }
}
