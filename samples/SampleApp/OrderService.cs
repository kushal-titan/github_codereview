using System;
using System.Threading;

namespace DeadlockSample
{
    internal class Program
    {
        private static readonly object ResourceA = new();
        private static readonly object ResourceB = new();

        private static readonly object ResourceX = new();
        private static readonly object ResourceY = new();

        private static void Main(string[] args)
        {
            Thread t1 = new(ProcessOne);
            Thread t2 = new(ProcessTwo);

            Thread t3 = new(TaskOne);
            Thread t4 = new(TaskTwo);

            t1.Start();
            t2.Start();

            t3.Start();
            t4.Start();

            t1.Join();
            t2.Join();

            t3.Join();
            t4.Join();

            Console.WriteLine("Completed");
        }

        private static void ProcessOne()
        {
            lock (ResourceA)
            {
                Console.WriteLine("ProcessOne locked ResourceA");
                Thread.Sleep(100);

                lock (ResourceB)
                {
                    Console.WriteLine("ProcessOne locked ResourceB");
                }
            }
        }

        private static void ProcessTwo()
        {
            lock (ResourceA)
            {
                Console.WriteLine("ProcessTwo locked ResourceA");
                Thread.Sleep(100);

                lock (ResourceB)
                {
                    Console.WriteLine("ProcessTwo locked ResourceB");
                }
            }
        }

        private static void TaskOne()
        {
            lock (ResourceX)
            {
                Console.WriteLine("TaskOne locked ResourceX");
                Thread.Sleep(100);

                lock (ResourceY)
                {
                    Console.WriteLine("TaskOne locked ResourceY");
                    PerformWork();
                }
            }
        }

        private static void TaskTwo()
        {
            lock (ResourceX)
            {
                Console.WriteLine("TaskTwo locked ResourceX");
                Thread.Sleep(100);

                lock (ResourceY)
                {
                    Console.WriteLine("TaskTwo locked ResourceY");
                    PerformWork();
                }
            }
        }

        private static void PerformWork()
        {
            for (int i = 0; i < 5; i++)
            {
                Console.WriteLine($"Working... {i}");
                Thread.Sleep(50);
            }
        }
    }
}