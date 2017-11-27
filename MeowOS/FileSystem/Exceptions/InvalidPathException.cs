using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowOS.FileSystem.Exceptions
{
    class InvalidPathException : Exception
    {
        public InvalidPathException(string path) : base("Путь " + path + " не существует.")
        {
        }
    }
}
