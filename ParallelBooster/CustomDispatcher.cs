using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ParallelBooster
{
    public class CustomDispatcher
    {
        private Action[] Actions { get; } = new Action[100000];
        private int Count { get; set; }
        private int Start { get; set; }
        public bool IsDone
        {
            get
            {
                lock(Lock)
                {
                    return Start == Count;
                }
            }
        }
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
            int currentCount;
            lock(Lock)
            {
                currentCount = Count;
            }

            for (int i = Start; i < currentCount; i += 1)
            {
                Actions[i]?.Invoke();
            }
            Start = currentCount;
        }
        public void Clear()
        {
            Count = 0;
            Start = 0;
        }
    }
}
