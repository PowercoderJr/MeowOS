namespace MeowOS
{
    public static class Session
    {
        public static UserInfo userInfo;

        public static void clear()
        {
            userInfo = null;
        }
    }
}
