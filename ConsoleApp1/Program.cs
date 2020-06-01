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
            sw = Stopwatch.StartNew();

            for (int i = 0; i < Count; i += 1)
            {
                actions[i] = new Action(() => Function(Loops));
            }
            foreach(var action in actions)
            {
                action();
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
