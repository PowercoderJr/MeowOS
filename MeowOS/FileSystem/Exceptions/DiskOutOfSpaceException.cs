using System;

namespace MeowOS.FileSystem.Exceptions
{
    class DiskOutOfSpaceException : Exception
    {
        public DiskOutOfSpaceException() : base("На диске нет места.")
        {

        }
    }
}
