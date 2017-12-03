using System.IO;

namespace MeowOS.FileSystem
{
    interface IConvertibleFromBytes
    {
        void fromByteArray(byte[] buffer);
        void fromByteStream(Stream input);
    }
}
