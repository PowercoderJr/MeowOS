using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlwnOS
{
    class UserInfo
    {
        public enum Roles { ADMIN, USER, GUEST };

        private int uid;
        public int Uid { get { return uid; } }
        private string login;
        public string Login { get { return login; } }
        private int gid;
        public int Gid { get { return gid; } }
        private string group;
        public string Group { get { return group; } }
        private Roles role;
        public Roles Role { get { return role; } }

        public UserInfo(int uid, string login, int gid, string group, Roles role)
        {
            this.uid = uid;
            this.login = login;
            this.gid = gid;
            this.group = group;
            this.role = role;
        }
    }
}
