namespace MeowOS
{
    public class UserInfo
    {
        public enum Roles { ADMIN, USER };
        public const string DEFAULT_GROUP = "main";

        private ushort uid;
        public ushort Uid => uid;
        private string login;
        public string Login
        {
            get => login;
            set => login = value;
        }
        private string digest;
        public string Digest
        {
            get => digest;
            set => digest = value;
        }
        //TODO 25.11: заменить gid и group на экземпляр GroupInfo?
        private ushort gid;
        public ushort Gid
        {
            get => gid;
            set => gid = value;
        }
        private string group;
        public string Group
        {
            get => group;
            set => group = value;
        }
        private Roles role;
        public Roles Role
        {
            get => role;
            set => role = value;
        }

        public UserInfo (UserInfo source)
        {
            this.uid = source.uid;
            this.login = source.login;
            this.digest = source.digest;
            this.gid = source.gid;
            this.group = source.group;
            this.role = source.role;
        }

        public UserInfo(ushort uid, string login, ushort gid, string group, Roles role)
        {
            this.uid = uid;
            this.login = login;
            this.gid = gid;
            this.group = group;
            this.role = role;
        }

        public UserInfo(ushort uid, string login, string digest, ushort gid, string group, Roles role)
        {
            this.uid = uid;
            this.login = login;
            this.digest = digest;
            this.gid = gid;
            this.group = group;
            this.role = role;
        }

        public override string ToString()
        {
            return login + UsefulThings.USERDATA_SEPARATOR +
                digest + UsefulThings.USERDATA_SEPARATOR +
                gid.ToString() + UsefulThings.USERDATA_SEPARATOR +
                role.ToString();
        }
    }
}
