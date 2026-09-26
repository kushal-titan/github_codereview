using System;
using System.Threading;

namespace DeadlockSample
{
    class Program
    {
        private static readonly object resourceA = new object();
        private static readonly object resourceB = new object();

        private static readonly object resourceX = new object();
        private static readonly object resourceY = new object();

        static void Main(string[] args)
        {
            Thread t1 = new Thread(ProcessOne);
            Thread t2 = new Thread(ProcessTwo);

            Thread t3 = new Thread(TaskOne);
            Thread t4 = new Thread(TaskTwo);

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

        static void ProcessOne()
        {
            lock (resourceA)
            {
                Console.WriteLine("ProcessOne locked resourceA");
                Thread.Sleep(100);

                lock (resourceB)
                {
                    Console.WriteLine("ProcessOne locked resourceB");
                }
            }
        }

        static void ProcessTwo()
        {
            lock (resourceB)
            {
                Console.WriteLine("ProcessTwo locked resourceB");
                Thread.Sleep(100);

                lock (resourceA)
                {
                    Console.WriteLine("ProcessTwo locked resourceA");
                }
            }
        }

        static void TaskOne()
        {
            lock (resourceX)
            {
                Console.WriteLine("TaskOne locked resourceX");
                Thread.Sleep(100);

                lock (resourceY)
                {
                    Console.WriteLine("TaskOne locked resourceY");
                    PerformWork();
                }
            }
        }

        static void TaskTwo()
        {
            lock (resourceY)
            {
                Console.WriteLine("TaskTwo locked resourceY");
                Thread.Sleep(100);

                lock (resourceX)
                {
                    Console.WriteLine("TaskTwo locked resourceX");
                    PerformWork();
                }
            }
        }

        static void PerformWork()
        {
            for (int i = 0; i < 5; i++)
            {
                Console.WriteLine($"Working... {i}");
                Thread.Sleep(50);
            }
        }
    }
}
