using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowOS.FileSystem.Exceptions
{
    class RootdirOutOfSpaceException : Exception
    {
        public RootdirOutOfSpaceException() : base("В корневом каталоге нет места.")
        {

        }
    }
}
