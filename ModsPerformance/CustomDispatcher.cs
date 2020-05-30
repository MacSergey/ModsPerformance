using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ModsPerformance
{
    public class CustomDispatcher
    {
        private List<Action> Actions { get; } = new List<Action>();
        private object Lock { get; } = new object();
        public void Add(Action action)
        {
            if (action == null)
                return;

            lock(Lock)
            {
                Actions.Add(action);
            }
        }
        public void Execute()
        {
            lock(Lock)
            {
                foreach(var action in Actions)
                {
                    action.Invoke();
                }
                Actions.Clear();
            }
        }
    }
}
