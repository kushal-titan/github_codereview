using System;
using System.Threading;
 
class Program
{
    static readonly object lock1 = new object();
    static readonly object lock2 = new object();
 
    static void Main()
    {
        Thread t1 = new Thread(Thread1);
        Thread t2 = new Thread(Thread2);
 
        t1.Start();
        t2.Start();
 
        t1.Join();
        t2.Join();
    }
 
    static void Thread1()
    {
        lock (lock1)
        {
            Console.WriteLine("Thread 1 acquired lock1");
 
            Thread.Sleep(1000); // Simulate some work
 
            Console.WriteLine("Thread 1 waiting for lock2");
 
            lock (lock2)
            {
                Console.WriteLine("Thread 1 acquired lock2");
            }
        }
    }
 
    static void Thread2()
    {
        lock (lock2)
        {
            Console.WriteLine("Thread 2 acquired lock2");
 
            Thread.Sleep(1000); // Simulate some work
 
            Console.WriteLine("Thread 2 waiting for lock1");
 
            lock (lock1)
            {
                Console.WriteLine("Thread 2 acquired lock1");
            }
        }
    }
}
 
