namespace MeowOS.ProcScheduler
{
    public class RRProcQueue : RRQueue<Process>
    {

        private Process.Priorities priority;
        public Process.Priorities Priority => priority;

        public RRProcQueue(int spinPeriod, Process.Priorities priority) : base(spinPeriod)
        {
            this.priority = priority;
        }
    }
}
