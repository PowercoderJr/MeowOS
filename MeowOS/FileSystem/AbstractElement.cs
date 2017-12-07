using System.IO;

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
