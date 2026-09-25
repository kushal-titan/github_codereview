using System;
using System.Threading;

namespace DeadlockSamples
{
    public class Program
    {
        private static readonly object LockA = new object();
        private static readonly object LockB = new object();

        private static readonly object LockC = new object();
        private static readonly object LockD = new object();

        private static readonly object LockE = new object();
        private static readonly object LockF = new object();

        private static readonly object LockG = new object();
        private static readonly object LockH = new object();

        static void Main(string[] args)
        {
            Console.WriteLine("Deadlock Sample Application");
        }

        public static void DeadlockScenario1()
        {
            Thread t1 = new Thread(() =>
            {
                lock (LockA)
                {
                    Thread.Sleep(100);
                    lock (LockB)
                    {
                        Console.WriteLine("Thread 1");
                    }
                }
            });

            Thread t2 = new Thread(() =>
            {
                lock (LockB)
                {
                    Thread.Sleep(100);
                    lock (LockA)
                    {
                        Console.WriteLine("Thread 2");
                    }
                }
            });

            t1.Start();
            t2.Start();
        }

        public static void DeadlockScenario2()
        {
            Thread t1 = new Thread(() =>
            {
                lock (LockC)
                {
                    Thread.Sleep(100);
                    lock (LockD)
                    {
                        Console.WriteLine("Process A");
                    }
                }
            });

            Thread t2 = new Thread(() =>
            {
                lock (LockD)
                {
                    Thread.Sleep(100);
                    lock (LockC)
                    {
                        Console.WriteLine("Process B");
                    }
                }
            });

            t1.Start();
            t2.Start();
        }

        public static void DeadlockScenario3()
        {
            Thread t1 = new Thread(() =>
            {
                lock (LockE)
                {
                    Thread.Sleep(100);
                    lock (LockF)
                    {
                        Console.WriteLine("Worker 1");
                    }
                }
            });

            Thread t2 = new Thread(() =>
            {
                lock (LockF)
                {
                    Thread.Sleep(100);
                    lock (LockE)
                    {
                        Console.WriteLine("Worker 2");
                    }
                }
            });

            t1.Start();
            t2.Start();
        }

        public static void DeadlockScenario4()
        {
            Thread t1 = new Thread(() =>
            {
                lock (LockG)
                {
                    Thread.Sleep(100);
                    lock (LockH)
                    {
                        Console.WriteLine("Task X");
                    }
                }
            });

            Thread t2 = new Thread(() =>
            {
                lock (LockH)
                {
                    Thread.Sleep(100);
                    lock (LockG)
                    {
                        Console.WriteLine("Task Y");
                    }
                }
            });

            t1.Start();
            t2.Start();
        }
    }
}
