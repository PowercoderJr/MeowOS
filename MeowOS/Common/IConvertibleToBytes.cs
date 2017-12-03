namespace MeowOS.FileSystem
{
    interface IConvertibleToBytes
    {
        byte[] toByteArray(bool expandToCluster);
    }
}
