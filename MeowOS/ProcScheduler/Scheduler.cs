using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowOS.ProcScheduler
{
    class Scheduler
    {
        public delegate void Log(string str);
        private Log log;
        public static readonly int[] QUANTUMS = { 4, 6, 8, 24 };
        public static readonly int QUEUE_AMOUNT = QUANTUMS.Length;
        public const int RRQ_SPIN_PERIOD = 5;
        public const int AVAILABLE_MEM = 1024;
        
        private int freeMem;
        private Process[] procs;
        public Process[] Procs => procs;
        private int[] bornTimes;
        public int[] BornTimes => bornTimes;
        private int procAmount;
        public int ProcAmount => procAmount;
        private int unitsAmount;
        public int UnitsAmount
        {
            get => unitsAmount;
            set => unitsAmount = value;
        }
        private RRQueue<RRProcQueue> rrq;
        private int currUnit;

        public void init(int procAmount, int maxBurst, Log log)
        {
            procs = new Process[procAmount];
            bornTimes = new int[procAmount];
            this.procAmount = procAmount;
            this.log = log;
            unitsAmount = 0;
            freeMem = AVAILABLE_MEM;

            Random rnd = new Random();
            for (int i = 0; i < procAmount; ++i)
            {
                int j;
                do
                {
                    j = rnd.Next(procAmount);
                } while (procs[j] != null);

                procs[j] = new Process(i + 1, (Process.Priorities)rnd.Next(Enum.GetNames(typeof(Process.Priorities)).Length),
                    rnd.Next(maxBurst) + 1, rnd.Next(AVAILABLE_MEM) + 1);
                bornTimes[j] = rnd.Next(unitsAmount) + 1;
                unitsAmount += procs[j].Burst;
                log("Сгенерирован процесс: " +
                    "PID = " + procs[j].PID +
                    ", приоритет = " + procs[j].Priority +
                    ", burst = " + procs[j].Burst +
                    ", потребляемая память = " + procs[j].MemRequired +
                    ", время появления = " + bornTimes[j]);
            }

            rrq = new RRQueue<RRProcQueue>(RRQ_SPIN_PERIOD);
            for (int i = QUEUE_AMOUNT - 1; i >= 0; --i)
                rrq.Enqueue(new RRProcQueue(QUANTUMS[i], (Process.Priorities)i));
            currUnit = 1;
        }

        public bool doUnit()
        {
            //TODO 02.12: обработать ситуацию, когда на данном шагу нет выполняющихся процессов, но currUnit < unitsAmount
            log("--- ШАГ " + currUnit + " ---");
            for (int i = 0; i < procAmount; ++i)
                if (bornTimes[i] == currUnit)
                {
                    enqProcByPriority(procs[i]);
                    procs[i].State = Process.States.CREATED;
                }

            bool unitDone = false;
            do
            {
                while (rrq.Peek().Count > 0 && rrq.Peek().Peek().State == Process.States.KILLED)
                    rrq.Peek().Dequeue();

                if (rrq.Peek().Count == 0)
                {
                    log("Очередь процессов с приоритетом " + rrq.Peek().Priority.ToString() +
                        " пуста, выполняется поиск следующей непустой очереди");
                    rrq.Spin();
                }
                else if (rrq.BeforeSpin == 0)
                {
                    log("Очередь процессов с приоритетом " + rrq.Peek().Priority.ToString() +
                        " отработала достаточно времени, выполняется поиск следующей непустой очереди");
                    rrq.Spin();
                }
                int qspins = 0;
                while (rrq.Peek().Count == 0 && qspins < QUEUE_AMOUNT)
                {
                    rrq.Spin();
                    ++qspins;
                }
                if (qspins == QUEUE_AMOUNT)
                {
                    log("Ни в одной из очередей нет процессов");
                    return false;
                }

                Process currProc = rrq.Peek().Peek();
                switch (currProc.State)
                {
                    case Process.States.CREATED:
                    case Process.States.WAITING:
                        if (currProc.MemRequired > freeMem)
                        {
                            string priorityIncreased = "";
                            Process.Priorities oldPriority = currProc.Priority;
                            if (currProc.Priority < Process.Priorities.HIGH)
                            {
                                priorityIncreased = " повысил свой приоритет и";
                                ++currProc.Priority;
                                enqProcByPriority(rrq.Peek().Dequeue());

                            }
                            else
                                rrq.Peek().Spin();

                            currProc.State = Process.States.WAITING;
                            log("Процесс " + currProc.PID + " (" + oldPriority + ")" + priorityIncreased +
                                " отправляется в конец очереди: требуется " + currProc.MemRequired +
                                " байт памяти, доступно всего " + freeMem);
                            --rrq.BeforeSpin;
                        }
                        else
                        {
                            log("Процесс " + currProc + " начал выполнение");
                            freeMem -= currProc.MemRequired;
                            currProc.State = Process.States.RUNNING;
                        }
                        break;
                    case Process.States.READY:
                    case Process.States.RUNNING:
                        --currProc.Burst;
                        log("Процесс " + currProc + " отработал одну единицу времени, осталось " + currProc.Burst);
                        if (currProc.Burst == 0)
                        {
                            log("Процесс " + currProc + " звершил выполнение");
                            rrq.Peek().Dequeue();
                            rrq.Peek().BeforeSpin = 0;
                            freeMem += currProc.MemRequired;
                            currProc.State = Process.States.KILLED;
                        }
                        else
                            --rrq.Peek().BeforeSpin;

                        if (rrq.Peek().BeforeSpin == 0)
                        {
                            log("Завершился очередной квант очереди процессов с приоритетом " + currProc.Priority);
                            rrq.Peek().Spin();
                            --rrq.BeforeSpin;
                        }
                        ++currUnit;
                        unitDone = true;
                        break;
                    default:
                        log("Не предусмотрено действий для состояния " + currProc.State);
                        break;
                }
            } while (!unitDone);
            return true;
        }

        public void clear()
        {
            procs = null;
            bornTimes = null;
            if (rrq != null)
                rrq.Clear();
            unitsAmount = 0;
            currUnit = 0;
        }

        private void enqProcByPriority(Process proc)
        {
            rrq.Find(item => item.Priority == proc.Priority).Enqueue(proc);
        }
    }
}
