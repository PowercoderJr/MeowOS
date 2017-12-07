using System;

namespace MeowOS.FileSystem.Exceptions
{
    class RootdirOutOfSpaceException : Exception
    {
        public RootdirOutOfSpaceException() : base("В корневом каталоге нет места.")
        {

        }
    }
}
