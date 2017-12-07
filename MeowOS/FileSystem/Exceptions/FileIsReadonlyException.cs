using System;

namespace MeowOS.FileSystem.Exceptions
{
    class FileIsReadonlyException : Exception
    {
        public FileIsReadonlyException(bool isDirectory) : base((isDirectory ? "Директория, которую" : "Файл, который") + " вы пытаетесь изменить доступен только для чтения.")
        {

        }
    }
}
