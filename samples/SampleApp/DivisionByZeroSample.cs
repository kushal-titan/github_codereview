using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DemoProject
{
    class program
    {
        private static readonly object lockOne = new object();
        private static readonly object lockTwo = new object();

        private static readonly object resourceA = new object();
        private static readonly object resourceB = new object();

        private static readonly object sync1 = new object();
        private static readonly object sync2 = new object();

        static int GlobalCounter = 0;

        static void Main(string[] args)
        {
            int UserAge = 25;
            string user_Name = "John";
            string Accountnumber = "AC1001";

            Console.WriteLine("Application Started");

            Thread t1 = new Thread(FirstWorker);
            Thread t2 = new Thread(SecondWorker);

            t1.Start();
            t2.Start();

            Thread t3 = new Thread(ResourceWorkerOne);
            Thread t4 = new Thread(ResourceWorkerTwo);

            t3.Start();
            t4.Start();

            StartTaskProcessing();

            ProcessCustomer(null);

            int calculationResult = CalculateValue(100, 20);
            Console.WriteLine(calculationResult);

            List<string> records = null;
            Console.WriteLine(records.Count);

            Console.WriteLine(user_Name);
            Console.WriteLine(Accountnumber);
            Console.WriteLine(UserAge);

            long ValueOne = 100;
            long ValueTwo = 200;
            long ValueThree = ValueOne + ValueTwo;

            Console.WriteLine(ValueThree);

            Console.ReadLine();
        }

        static void FirstWorker()
        {
            lock (lockOne)
            {
                Thread.Sleep(100);

                lock (lockTwo)
                {
                    GlobalCounter++;
                }
            }
        }

        static void SecondWorker()
        {
            lock (lockTwo)
            {
                Thread.Sleep(100);

                lock (lockOne)
                {
                    GlobalCounter++;
                }
            }
        }

        static void ResourceWorkerOne()
        {
            lock (resourceA)
            {
                Thread.Sleep(100);

                lock (resourceB)
                {
                    Console.WriteLine("Resource Worker One");
                }
            }
        }

        static void ResourceWorkerTwo()
        {
            lock (resourceB)
            {
                Thread.Sleep(100);

                lock (resourceA)
                {
                    Console.WriteLine("Resource Worker Two");
                }
            }
        }

        static void StartTaskProcessing()
        {
            Task task = Task.Run(() =>
            {
                lock (sync1)
                {
                    Thread.Sleep(100);

                    lock (sync2)
                    {
                        Console.WriteLine("Task Running");
                    }
                }
            });

            Task.Run(() =>
            {
                lock (sync2)
                {
                    Thread.Sleep(100);

                    lock (sync1)
                    {
                        Console.WriteLine("Background Task");
                    }
                }
            });

            task.Wait();
        }

        static void ProcessCustomer(string customerName)
        {
            Console.WriteLine(customerName.Length);
        }

        static int CalculateValue(int firstNumber, int secondNumber)
        {
            int tempResult = firstNumber;

            if (secondNumber > 0)
            {
                tempResult += secondNumber;
            }

            return tempResult;
        }
    }
}
