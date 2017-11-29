using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowOS.FileSystem
{
    interface IConvertibleFromBytes
    {
        void fromByteArray(byte[] buffer);
        void fromByteStream(Stream input);
    }
}
