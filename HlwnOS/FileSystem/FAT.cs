using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HlwnOS.FileSystem
{
    class FAT : AbstractElement
    {
        public const ushort CL_FREE         = 0x0000;
        public const ushort CL_SYSTEM       = 0xFFF0;
        public const ushort CL_ROOTDIR      = 0xFFF1;
        public const ushort CL_WRITING      = 0xFFF2;
        public const ushort CL_BAD          = 0xFFF7;
        public const ushort CL_EOF          = 0xFFFF;

        public const int ELEM_SIZE = sizeof(ushort);
        private ushort[] table;
        public ushort[] Table
        {
            get { return table; }
            set { table = value; }
        }
        private int tableSize;
        public int TableSize
        {
            get { return tableSize; }
        }
        private int freeClustersCount;

        public FAT(FileManager fm, int tableSize)
        {
            this.fm = fm;

            this.tableSize = tableSize;
            table = new ushort[this.tableSize];
            uint systemArea = fm.SuperBlock.RootOffset / fm.SuperBlock.ClusterSize;
            for (uint i = 0; i < systemArea; ++i)
                table[i] = CL_SYSTEM;
            uint rootdirArea = (fm.SuperBlock.DataOffset - fm.SuperBlock.RootOffset) / fm.SuperBlock.ClusterSize;
            for (uint i = systemArea; i < systemArea + rootdirArea; ++i)
                table[i] = CL_ROOTDIR;
        }
        
        public override byte[] toByteArray(bool expandToCluster)
        {
            byte[] buffer = new byte[expandToCluster ? fm.SuperBlock.Fat2Offset - fm.SuperBlock.Fat1Offset : tableSize * ELEM_SIZE];
            Buffer.BlockCopy(table, 0, buffer, 0, tableSize * ELEM_SIZE);
            return buffer;

            /*
            //ArrayList buffer = new ArrayList();
            //Запись информации
            foreach (ushort elem in table)
                bw.Write(elem);

            //Заполнение оставшегося места
            int endingSize = fm.SuperBlock.ClusterSize - table.Length * ELEM_SIZE % fm.SuperBlock.ClusterSize;
            for (int i = 0; i < endingSize; ++i)
                bw.Write((byte)0);*/                
        }

        public override void fromByteArray(byte[] buffer)
        {
            throw new NotImplementedException();
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
