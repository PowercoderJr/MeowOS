using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlwnOS.FileSystem
{
    public class FAT : AbstractElement
    {
        public const ushort CL_FREE         = 0x0000;
        public const ushort CL_SYSTEM       = 0xFFF0;
        public const ushort CL_ROOTDIR      = 0xFFF1;
        public const ushort CL_WRITING      = 0xFFF2;
        public const ushort CL_BAD          = 0xFFF7;
        public const ushort CL_EOF          = 0xFFFF;

        public const int ELEM_SIZE = sizeof(ushort);
        private ushort[] table;
        public ushort[] Table => table;
        private int tableSize;
        public int TableSize
        {
            get { return tableSize; }
        }

        public FAT(Controller ctrl, int tableSize, Stream input)
        {
            this.ctrl = ctrl;
            this.tableSize = tableSize;
            table = new ushort[this.tableSize];
            fromByteStream(input);
        }

        public FAT(Controller ctrl, int tableSize)
        {
            this.ctrl = ctrl;
            this.tableSize = tableSize;
            table = new ushort[this.tableSize];
            uint systemArea = ctrl.SuperBlock.RootOffset / ctrl.SuperBlock.ClusterSize;
            uint i;
            for (i = 0; i < systemArea; ++i)
                table[i] = CL_SYSTEM;
            uint rootdirArea = (ctrl.SuperBlock.DataOffset - ctrl.SuperBlock.RootOffset) / ctrl.SuperBlock.ClusterSize;
            for (i = systemArea; i < systemArea + rootdirArea; ++i)
                table[i] = (ushort)(i + 1);
            table[i - 1] = CL_EOF;
        }
        
        public override byte[] toByteArray(bool expandToCluster)
        {
            byte[] buffer = new byte[expandToCluster ? ctrl.SuperBlock.Fat2Offset - ctrl.SuperBlock.Fat1Offset : tableSize * ELEM_SIZE];
            Buffer.BlockCopy(table, 0, buffer, 0, tableSize * ELEM_SIZE);
            return buffer;
        }

        public override void fromByteArray(byte[] buffer)
        {
            Buffer.BlockCopy(buffer, 0, table, 0, tableSize * ELEM_SIZE);
        }

        public override void fromByteStream(Stream input)
        {
            BinaryReader br = new BinaryReader(input);
            fromByteArray(br.ReadBytes(tableSize * ELEM_SIZE));
        }

        public override string ToString()
        {
            string result = "";
            const int IN_STRING = 8;
            for (int i = 0; i < tableSize / IN_STRING; ++i)
            {
                for (int j = 0; j < IN_STRING; ++j)
                    result += String.Format("{0, -6}", table[i * IN_STRING + j]);
                result += '\n';
            }
            return result;
        }

        public ushort getFreeClusterIndex()
        {
            ushort i;
            for (i = 0; i < tableSize && table[i] != CL_FREE; ++i);
            //return i < tableSize ? i : (ushort)(tableSize+1);
            return i; //Если свободных кластеров нет, возвращает значение, выходящее на границы table
        }
    }
}
