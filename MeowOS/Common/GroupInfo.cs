using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowOS.Common
{
    public class GroupInfo
    {
        private int id;
        public int Id => id;
        private string name;
        public string Name
        {
            get => name;
            set => name = value;
        }

        public GroupInfo(int id, string name)
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
