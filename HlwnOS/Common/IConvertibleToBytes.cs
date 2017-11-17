using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlwnOS.FileSystem
{
    interface IConvertibleToBytes
    {
        byte[] toByteArray(bool expandToCluster);
    }
}
