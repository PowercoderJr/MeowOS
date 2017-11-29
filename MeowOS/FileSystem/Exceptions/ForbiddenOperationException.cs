using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowOS.FileSystem.Exceptions
{
    class ForbiddenOperationException : Exception
    {
        public ForbiddenOperationException() : base("Операция запрещена.")
        {

        }
    }
}
