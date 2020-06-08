using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace ConsoleApp1
{
    class Program
    {
        static int Count => 10000;
        static int Loops => 1000;
        static Delegate ActionDelegate { get; } = new Action<int>(Function);

        static void Main(string[] args)
        {
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < Count; i += 1)
            {
                Function(Loops);
            }

            sw.Stop();
            Console.WriteLine(sw.ElapsedTicks);

            var actions = new Action[Count];
            for (int i = 0; i < Count; i += 1)
            {
                actions[i] = new Action(() => Function(Loops));
            }
            sw = Stopwatch.StartNew();
            foreach (var action in actions)
            {
                action();
            }

            sw.Stop();
            Console.WriteLine(sw.ElapsedTicks);

            var delegates = new Delegate[Count];
            var argsArr = new object[Count][];
            for (int i = 0; i < Count; i += 1)
            {
                delegates[i] = ActionDelegate;
                argsArr[i] = new object[] { Loops };
            }
            sw = Stopwatch.StartNew();
            for (int i = 0; i < delegates.Length; i += 1)
            {
                delegates[i].DynamicInvoke(argsArr[i]);
            }

            sw.Stop();
            Console.WriteLine(sw.ElapsedTicks);

            var actions2 = new Action<object[]>[Count];
            argsArr = new object[Count][];
            for (int i = 0; i < Count; i += 1)
            {
                actions2[i] = new Action<object[]>((arg) => Function((int)arg[0]));
                argsArr[i] = new object[] { Loops };
            }
            sw = Stopwatch.StartNew();
            for (int i = 0; i < actions2.Length; i += 1)
            {
                actions2[i].Invoke(argsArr[i]);
            }

            sw.Stop();
            Console.WriteLine(sw.ElapsedTicks);

            Console.ReadKey();
        }

        static void Function(int loops)
        {
            for(int i = 0; i < loops; i += 1)
            {

            }
        }
    }
}
