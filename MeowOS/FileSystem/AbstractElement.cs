using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MeowOS.FileSystem;

namespace MeowOS.FileSystem
{
    public abstract class AbstractElement : IConvertibleToBytes, IConvertibleFromBytes
    {
        protected FileSystemController fsctrl;

        public abstract byte[] toByteArray(bool expandToCluster);
        public abstract void fromByteArray(byte[] buffer);
        public abstract void fromByteStream(Stream input);
    }
}
