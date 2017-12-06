using System;
using System.Collections.Generic;

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
        public int FreeMem
        {
            get => freeMem;
            set => freeMem = value;
        }
        private Process[] procs;
        public Process[] Procs => procs;
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
            this.procAmount = procAmount;
            this.log = log;
            unitsAmount = 0;
            freeMem = AVAILABLE_MEM;
            quantumEndedFlag = false;

            Random rnd = new Random();
            for (int i = 0; i < procAmount; ++i)
            {
                procs[i] = new Process(i + 1, (Process.Priorities)rnd.Next(Enum.GetNames(typeof(Process.Priorities)).Length),
                    rnd.Next(maxBurst) + 1, rnd.Next(AVAILABLE_MEM) + 1, rnd.Next(unitsAmount) + 1);
                unitsAmount += procs[i].Burst;
                log("Сгенерирован процесс: " +
                    "PID = " + procs[i].PID +
                    ", приоритет = " + procs[i].Priority +
                    ", burst = " + procs[i].Burst +
                    ", потребляемая память = " + procs[i].MemRequired +
                    ", время появления = " + procs[i].BornTime);
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
                if (procs[i].BornTime == currUnit && procs[i].State == Process.States.UNBORN)
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
                            bool memFound = currProc.MemRequired <= freeMem;

                            if (!memFound && currProc.Priority == Process.Priorities.ABSOLUTE)
                            {
                                List<Process> poorProcs = new List<Process>();
                                int potentialMem = freeMem;
                                bool memAdded;
                                do
                                {
                                    memAdded = false;
                                    for (int i = (int)Process.Priorities.LOW; !memAdded && i < (int)Process.Priorities.ABSOLUTE; ++i)
                                    {
                                        RRProcQueue rrpq = rrq.Find(q => q.Priority == (Process.Priorities)i);
                                        Process poorProc = rrpq.FindLast(p => p.State == Process.States.READY && !poorProcs.Contains(p));
                                        if (poorProc != null)
                                        {
                                            poorProcs.Add(poorProc);
                                            potentialMem += poorProc.MemRequired;
                                            memAdded = true;
                                        }
                                    }
                                } while (memAdded && currProc.MemRequired > potentialMem);

                                if (currProc.MemRequired <= potentialMem)
                                {
                                    poorProcs.Sort((p1, p2) => p2.MemRequired - p1.MemRequired);
                                    int i = 0;
                                    while (i < poorProcs.Count)
                                    {
                                        if (currProc.MemRequired <= potentialMem - poorProcs[i].MemRequired)
                                            poorProcs.RemoveAt(i);
                                        else
                                            ++i;
                                    }

                                    foreach (Process poorProc in poorProcs)
                                    {
                                        poorProc.State = Process.States.WAITING;
                                        freeMem += poorProc.MemRequired;
                                        log("Процесс " + currProc + " вытеснил процесс " + poorProc + " (" +
                                            poorProc.MemRequired + " байт памяти освобождено, теперь свободно " + freeMem + " байт)");
                                        //TODO повышать приоритет
                                        if (currProc.Priority < Process.Priorities.ABSOLUTE - 1)
                                        {
                                            Process.Priorities oldPriority = currProc.Priority;
                                            ++currProc.Priority;
                                            log("Процесс " + currProc.PID + " (" + oldPriority + ") повысил свой приоритет до " +
                                                currProc.Priority + " и отправляется в конец соответствующей очереди");
                                        }
                                    }
                                    memFound = true;
                                }
                            }

                            if (memFound)
                            {
                                currProc.State = Process.States.RUNNING;
                                freeMem -= currProc.MemRequired;
                                log("Процесс " + currProc + " начал выполнение (" + currProc.MemRequired +
                                    " байт памяти выделено, теперь свободно " + freeMem + " байт)");
                            }
                            else
                            {
                                currProc.State = Process.States.WAITING;
                                string priorityIncreased = "";
                                Process.Priorities oldPriority = currProc.Priority;
                                if (currProc.Priority < Process.Priorities.ABSOLUTE - 1)
                                {
                                    ++currProc.Priority;
                                    priorityIncreased = " повысил свой приоритет до " + currProc.Priority + " и";
                                    enqProcByPriority(rrq.Peek().Dequeue());

                                }
                                else
                                    rrq.Peek().Spin();

                                log("Процесс " + currProc.PID + " (" + oldPriority + ")" + priorityIncreased +
                                    " отправляется в конец очереди: требуется " + currProc.MemRequired +
                                    " байт памяти, доступно всего " + freeMem);
                                --rrq.BeforeSpin;
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
                                rrq.Peek().Dequeue();
                                rrq.Peek().BeforeSpin = 0;
                                freeMem += currProc.MemRequired;
                                log("Процесс " + currProc + " звершил выполнение (" + currProc.MemRequired +
                                    " байт памяти освобождено, теперь свободно " + freeMem + " байт)");
                            }
                            else
                                --rrq.Peek().BeforeSpin;

                            quantumEndedFlag = rrq.Peek().BeforeSpin == 0;
                            if (quantumEndedFlag)
                            {
                                if (currProc.State == Process.States.RUNNING)
                                    currProc.State = Process.States.READY;

                                if (currProc.Priority != currProc.EffPriority)
                                {
                                    Process.Priorities oldPriority = currProc.Priority;
                                    currProc.Priority = currProc.EffPriority;
                                    enqProcByPriority(rrq.Peek().Dequeue());
                                    string tolog = "Процесс " + currProc.PID + " (" + oldPriority + ") вернул приоритет " + currProc.Priority;
                                    if (currProc.Burst > 0)
                                        tolog += " и помещён в конец соответствующей очереди";
                                    log(tolog + " (имел повышенный приоритет, т.к. в прошлую его очередь ему не досталось ресурсов)");
                                }

                                endCurrQuantum();
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
            if (rrq != null)
                rrq.Clear();
            unitsAmount = 0;
            currUnit = 0;
            quantumEndedFlag = false;
            freeMem = AVAILABLE_MEM;
        }

        public void endCurrQuantum()
        {
            log("Завершился очередной квант очереди процессов с приоритетом " + rrq.Peek().Priority);
            rrq.Peek().Spin();
            --rrq.BeforeSpin;
        }

        public void enqProcByPriority(Process proc)
        {
            rrq.Find(q => q.Priority == proc.Priority).Enqueue(proc);
        }

        public bool deqProc(Process proc)
        {
            bool removed = false;
            for (int i = 0; i < rrq.Count && !removed; ++i)
                removed = rrq[i].Remove(proc);
            return removed;
        }
    }
}
