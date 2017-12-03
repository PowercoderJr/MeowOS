using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeowOS.ProcScheduler
{
    public class Scheduler
    {
        public delegate void Log(string str);
        private Log log;
        public static readonly int[] QUANTUMS = { 4, 6, 8, 24 };
        public static readonly int QUEUE_AMOUNT = QUANTUMS.Length;
        public const int RRQ_SPIN_PERIOD = 5;
        public const int AVAILABLE_MEM = 1024;
        
        private int freeMem;
        public int FreeMem => freeMem;
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
        private int currUnit;
        public int CurrUnit => currUnit;
        private RRQueue<RRProcQueue> rrq;

        private bool quantumEndedFlag;
        public bool QuantumEndedFlag => quantumEndedFlag;

        public void init(int procAmount, int maxBurst, Log log)
        {
            procs = new Process[procAmount];
            bornTimes = new int[procAmount];
            this.procAmount = procAmount;
            this.log = log;
            unitsAmount = 0;
            freeMem = AVAILABLE_MEM;
            quantumEndedFlag = false;

            Random rnd = new Random();
            for (int i = 0; i < procAmount; ++i)
            {
                procs[i] = new Process(i + 1, (Process.Priorities)rnd.Next(Enum.GetNames(typeof(Process.Priorities)).Length),
                    rnd.Next(maxBurst) + 1, rnd.Next(AVAILABLE_MEM) + 1);
                bornTimes[i] = rnd.Next(unitsAmount) + 1;
                unitsAmount += procs[i].Burst;
                log("Сгенерирован процесс: " +
                    "PID = " + procs[i].PID +
                    ", приоритет = " + procs[i].Priority +
                    ", burst = " + procs[i].Burst +
                    ", потребляемая память = " + procs[i].MemRequired +
                    ", время появления = " + bornTimes[i]);
            }

            rrq = new RRQueue<RRProcQueue>(RRQ_SPIN_PERIOD);
            for (int i = QUEUE_AMOUNT - 1; i >= 0; --i)
                rrq.Enqueue(new RRProcQueue(QUANTUMS[i], (Process.Priorities)i));
            currUnit = 1;
        }

        public int doUnit()
        {
            log("--- ШАГ " + currUnit + " ---");
            for (int i = 0; i < procAmount; ++i)
                if (bornTimes[i] == currUnit && procs[i].State == Process.States.UNBORN)
                {
                    procs[i].State = Process.States.BORN;
                    enqProcByPriority(procs[i]);
                    log("Процесс " + procs[i] + " родился");
                }

            bool unitDone = false;
            int activePID = -1;
            do
            {
                while (rrq.Peek().Count > 0 && (rrq.Peek().Peek().State == Process.States.COMPLETED || rrq.Peek().Peek().State == Process.States.KILLED))
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
                int qSpins = 0;
                while (rrq.Peek().Count == 0 && qSpins < QUEUE_AMOUNT)
                {
                    rrq.Spin();
                    ++qSpins;
                }
                if (qSpins == QUEUE_AMOUNT)
                {
                    log("Ни в одной из очередей нет процессов");
                    unitDone = true;
                }

                if (!unitDone)
                {
                    Process currProc = rrq.Peek().Peek();
                    switch (currProc.State)
                    {
                        case Process.States.BORN:
                        case Process.States.WAITING:
                            if (currProc.MemRequired > freeMem)
                            {
                                currProc.State = Process.States.WAITING;
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

                                log("Процесс " + currProc.PID + " (" + oldPriority + ")" + priorityIncreased +
                                    " отправляется в конец очереди: требуется " + currProc.MemRequired +
                                    " байт памяти, доступно всего " + freeMem);
                                --rrq.BeforeSpin;
                            }
                            else
                            {
                                currProc.State = Process.States.RUNNING;
                                log("Процесс " + currProc + " начал выполнение (" + currProc.MemRequired + " байт памяти выделено)");
                                freeMem -= currProc.MemRequired;
                            }
                            break;
                        case Process.States.READY:
                        case Process.States.RUNNING:
                            currProc.State = Process.States.RUNNING;
                            --currProc.Burst;
                            log("Процесс " + currProc + " отработал одну единицу времени, осталось " + currProc.Burst);
                            if (currProc.Burst == 0)
                            {
                                currProc.State = Process.States.COMPLETED;
                                log("Процесс " + currProc + " звершил выполнение (" + currProc.MemRequired + " байт памяти освобождено)");
                                rrq.Peek().Dequeue();
                                rrq.Peek().BeforeSpin = 0;
                                freeMem += currProc.MemRequired;
                            }
                            else
                                --rrq.Peek().BeforeSpin;

                            quantumEndedFlag = rrq.Peek().BeforeSpin == 0;
                            if (quantumEndedFlag)
                            {
                                if (currProc.State == Process.States.RUNNING)
                                    currProc.State = Process.States.READY;
                                log("Завершился очередной квант очереди процессов с приоритетом " + currProc.Priority);
                                rrq.Peek().Spin();
                                --rrq.BeforeSpin;
                            }
                            unitDone = true;
                            activePID = currProc.PID;
                            break;
                        default:
                            log("Не предусмотрено действий для состояния " + currProc.State);
                            break;
                    }
                }
            } while (!unitDone);
            ++currUnit;
            return activePID;
        }

        public void clear()
        {
            procs = null;
            bornTimes = null;
            if (rrq != null)
                rrq.Clear();
            unitsAmount = 0;
            currUnit = 0;
            quantumEndedFlag = false;
        }

        private void enqProcByPriority(Process proc)
        {
            rrq.Find(item => item.Priority == proc.Priority).Enqueue(proc);
        }
    }
}
