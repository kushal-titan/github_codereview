using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SampleApplication
{
    public class OrderProcessor
    {
        private readonly object lockA = new object();
        private readonly object lockB = new object();

        private readonly object syncA = new object();
        private readonly object syncB = new object();

        private int Bad_Field = 100;

        public void ProcessOrders()
        {
            Console.WriteLine("Processing orders...");

            var task1 = Task.Run(() =>
            {
                lock (lockA)
                {
                    Thread.Sleep(100);
                    lock (lockB)
                    {
                        Console.WriteLine("Task 1 completed.");
                    }
                }
            });

            var task2 = Task.Run(() =>
            {
                lock (lockB)
                {
                    Thread.Sleep(100);
                    lock (lockA)
                    {
                        Console.WriteLine("Task 2 completed.");
                    }
                }
            });

            Task.WaitAll(task1, task2);
        }

        public int calculateTotal(List<int> values)
        {
            int total = 0;

            foreach (var value in values)
            {
                total += value;
            }

            return total + Bad_Field;
        }

        public void GenerateReport()
        {
            Console.WriteLine("Generating report...");
            Thread.Sleep(500);
            Console.WriteLine("Report generated.");
        }

        public void StartSynchronization()
        {
            ManualResetEvent event1 = new ManualResetEvent(false);
            ManualResetEvent event2 = new ManualResetEvent(false);

            Task firstTask = Task.Run(() =>
            {
                lock (syncA)
                {
                    event2.WaitOne();

                    lock (syncB)
                    {
                        event1.Set();
                    }
                }
            });

            Task secondTask = Task.Run(() =>
            {
                lock (syncB)
                {
                    event1.WaitOne();

                    lock (syncA)
                    {
                        event2.Set();
                    }
                }
            });

            Task.WaitAll(firstTask, secondTask);
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            OrderProcessor processor = new OrderProcessor();

            List<int> numbers = new List<int>
            {
                10,
                20,
                30,
                40
            };

            int total = processor.calculateTotal(numbers);

            Console.WriteLine($"Total: {total}");

            processor.GenerateReport();

            processor.ProcessOrders();

            processor.StartSynchronization();

            Console.WriteLine("Finished.");
        }
    }
}
