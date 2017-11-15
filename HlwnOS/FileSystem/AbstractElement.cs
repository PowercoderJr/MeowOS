using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HlwnOS.FileSystem;

namespace HlwnOS.FileSystem
{
    abstract class AbstractElement : IConvertableToBytes, IConvertableFromBytes
    {
        protected Controller ctrl;

        public abstract byte[] toByteArray(bool expandToCluster);
        public abstract void fromByteArray(byte[] buffer);
        public abstract void fromByteStream(Stream input);
    }
}
