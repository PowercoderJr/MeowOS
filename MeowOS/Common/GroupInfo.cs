namespace MeowOS.Common
{
    public class GroupInfo
    {
        private ushort id;
        public ushort Id => id;
        private string name;
        public string Name
        {
            get => name;
            set => name = value;
        }

        public GroupInfo(ushort id, string name)
        {
            this.id = id;
            this.name = name;
        }

        public override string ToString()
        {
            return name;
        }
    }
}
