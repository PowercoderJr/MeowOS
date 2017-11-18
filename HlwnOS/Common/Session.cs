using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlwnOS
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
