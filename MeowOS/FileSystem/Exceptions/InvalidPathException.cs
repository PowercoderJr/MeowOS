using System;

namespace MeowOS.FileSystem.Exceptions
{
    class InvalidPathException : Exception
    {
        public InvalidPathException(string path) : base("Путь " + path + " не существует.")
        {
        }
    }
}
