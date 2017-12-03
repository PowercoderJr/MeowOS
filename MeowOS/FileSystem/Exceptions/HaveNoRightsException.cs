using System;

namespace MeowOS.FileSystem.Exceptions
{
    public class HaveNoRightsException : Exception
    {
        public enum Rights { R_READ, R_WRITE, R_EXECUTE };
        public HaveNoRightsException(Rights rights) : base("Нет прав на " + (rights == Rights.R_READ ? "чтение" : 
            (rights == Rights.R_WRITE ? "запись" : "выполнение")) + " по заданному пути.")
        {

        }
    }
}
