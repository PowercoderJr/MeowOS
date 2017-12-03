using System;
using System.Collections.Generic;

namespace MeowOS.ProcScheduler
{
    public class RRQueue<T> : List<T>
    {
        new public void Add(T item) { throw new NotSupportedException(); }
        new public void AddRange(IEnumerable<T> collection) { throw new NotSupportedException(); }
        new public void Insert(int index, T item) { throw new NotSupportedException(); }
        new public void InsertRange(int index, IEnumerable<T> collection) { throw new NotSupportedException(); }
        new public void Reverse() { throw new NotSupportedException(); }
        new public void Reverse(int index, int count) { throw new NotSupportedException(); }
        new public void Sort() { throw new NotSupportedException(); }
        new public void Sort(Comparison<T> comparison) { throw new NotSupportedException(); }
        new public void Sort(IComparer<T> comparer) { throw new NotSupportedException(); }
        new public void Sort(int index, int count, IComparer<T> comparer) { throw new NotSupportedException(); }

        protected int spinPeriod;
        public int SpinPeriod => spinPeriod;
        protected int beforeSpin;
        public int BeforeSpin
        {
            get => beforeSpin;
            set => beforeSpin = value;
        }

        public RRQueue(int spinPeriod) : base()
        {
            this.spinPeriod = this.beforeSpin = spinPeriod;
        }

        public void Enqueue(T item)
        {
            base.Add(item);
        }

        public T Dequeue()
        {
            var t = base[0];
            RemoveAt(0);
            return t;
        }

        public T Peek()
        {
            return base[0];
        }

        public void Spin()
        {
            if (Count > 1)
                Enqueue(Dequeue());
            beforeSpin = spinPeriod;
        }
    }
}
