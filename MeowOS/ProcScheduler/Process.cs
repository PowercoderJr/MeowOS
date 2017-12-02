using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowOS.ProcScheduler
{
    class Process
    {
        public enum Priorities { ABSOLUTE = 0, HIGH = 1, NORMAL = 2, LOW = 3 };
        public enum States { UNBORN, CREATED, READY, RUNNING, WAITING, KILLED }

        private int pid;
        public int PID => pid;

        private Priorities priority;
        public Priorities Priority
        {
            get => priority;
            set => priority = value;
        }

        private States state;
        public States State
        {
            get => state;
            set => state = value;
        }

        private int burst;
        public int Burst => burst;

        private int memRequired;
        public int MemRequired => memRequired;

        public Process()
        {
            state = States.UNBORN;
        }

        public Process(int pid, Priorities priority, int burst, int memRequired) : this()
        {
            this.pid = pid;
            this.priority = priority;
            this.burst = burst;
            this.memRequired = memRequired;
        }
    }
}
