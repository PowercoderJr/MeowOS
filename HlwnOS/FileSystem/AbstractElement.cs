using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HlwnOS.FileSystem;

namespace HlwnOS.FileSystem
{
    abstract class AbstractElement : IConvertableToByteArray, IConvertableFromByteArray
    {
        protected FileManager fm;

        public abstract byte[] toByteArray(bool expandToCluster);
        public abstract void fromByteArray(byte[] buffer);
    }
}
