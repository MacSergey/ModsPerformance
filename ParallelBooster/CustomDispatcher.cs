using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ParallelBooster
{
    public class CustomDispatcher
    {
        private Action[] Actions { get; } = new Action[100000];
        public int Count { get; private set; }
        public int Executed { get; private set; }
        public bool NothingExecute => Executed == Count;
        //{
        //    get
        //    {
        //        lock(Lock)
        //        {
        //            return Start == Count;
        //        }
        //    }
        //}
        private object Lock { get; } = new object();

        public CustomDispatcher()
        {
            Clear();
        }

        public void Add(Action action)
        {
            if (action == null)
                return;

            int index;
            lock(Lock)
            {
                index = Count;
                Count += 1;
            }

            Actions[index] = action;
        }
        public void Execute()
        {
            if (NothingExecute)
                return;

            int currentCount;
            lock(Lock)
            {
                currentCount = Count;
            }
            //Logger.Debug($"Execute from #{Start} to #{currentCount}");
            for (int i = Executed; i < currentCount; i += 1)
            {
                Actions[i]?.Invoke();
            }
            Executed = currentCount;
        }
        public void Clear()
        {
            Count = 0;
            Executed = 0;
        }
    }
}
