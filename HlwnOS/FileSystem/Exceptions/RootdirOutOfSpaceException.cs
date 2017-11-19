using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlwnOS.FileSystem.Exceptions
{
    class RootdirOutOfSpaceException : Exception
    {
        public RootdirOutOfSpaceException() : base("В корневом каталоге нет места")
        {

        }
    }
}
