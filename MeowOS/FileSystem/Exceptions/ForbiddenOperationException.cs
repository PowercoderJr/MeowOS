using System;

namespace MeowOS.FileSystem.Exceptions
{
    class ForbiddenOperationException : Exception
    {
        public ForbiddenOperationException() : base("Операция запрещена.")
        {

        }
    }
}
