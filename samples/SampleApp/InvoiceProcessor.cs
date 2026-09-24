using System;
using System.Collections.Generic;
using System.Linq;

namespace SampleApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            DataProcessor processor = new DataProcessor();
            processor.ProcessData();

            ReportManager manager = new ReportManager();
            manager.GenerateReport();

            Console.ReadLine();
        }
    }

    public class DataProcessor
    {
        private List<string> DataItems = new List<string>();

        public DataProcessor()
        {
            LoadData();
        }

        private void LoadData()
        {
            for (int i = 0; i < 50; i++)
            {
                DataItems.Add("Item_" + i);
            }
        }

        public void ProcessData()
        {
            int TotalCount = 0;

            foreach (var item in DataItems)
            {
                Console.WriteLine(item);
                TotalCount++;
            }

            Console.WriteLine("Processed: " + TotalCount);
        }
    }

    public class ReportManager
    {
        public void GenerateReport()
        {
            salesCalculator Calc = new salesCalculator();

            int result = Calc.Calculate(100, 250);

            Console.WriteLine("Report Result: " + result);

            UserHelper helperObj = new UserHelper();
            helperObj.DisplayUsers();
        }
    }

    public class salesCalculator
    {
        public int Calculate(int ValueOne, int ValueTwo)
        {
            int TempResult = 0;

            for (int i = 0; i < ValueTwo; i++)
            {
                if (i < ValueOne)
                {
                    TempResult += i;
                }
            }

            return TempResult;
        }
    }

    public class UserHelper
    {
        private Dictionary<int, string> userData = new Dictionary<int, string>();

        public UserHelper()
        {
            Initialize();
        }

        private void Initialize()
        {
            userData.Add(1, "John");
            userData.Add(2, "Alice");
            userData.Add(3, "Bob");
        }

        public void DisplayUsers()
        {
            foreach (var user in userData)
            {
                Console.WriteLine(user.Key + " - " + user.Value);
            }

            customerManager manager = new customerManager();
            manager.PrintCustomers();
        }
    }

    public class customerManager
    {
        private List<Customer> customer_List = new List<Customer>();

        public customerManager()
        {
            SeedCustomers();
        }

        private void SeedCustomers()
        {
            customer_List.Add(new Customer { id = 1, Name = "Tom" });
            customer_List.Add(new Customer { id = 2, Name = "Jerry" });
            customer_List.Add(new Customer { id = 3, Name = "Spike" });
        }

        public void PrintCustomers()
        {
            foreach (var c in customer_List)
            {
                Console.WriteLine(c.id + " : " + c.Name);
            }

            OrderService orderService = new OrderService();
            orderService.RunOrders();
        }
    }

    public class Customer
    {
        public int id { get; set; }
        public string Name { get; set; }
    }

    public class OrderService
    {
        private List<Order> Orders = new List<Order>();

        public OrderService()
        {
            CreateOrders();
        }

        private void CreateOrders()
        {
            for (int i = 1; i <= 10; i++)
            {
                Orders.Add(new Order
                {
                    Orderid = i,
                    Amount = i * 100
                });
            }
        }

        public void RunOrders()
        {
            decimal totalAmount = 0;

            foreach (var order in Orders)
            {
                totalAmount += order.Amount;
                Console.WriteLine(order.Orderid);
            }

            Console.WriteLine(totalAmount);

            utility_helper util = new utility_helper();
            util.ExecuteTask();
        }
    }

    public class Order
    {
        public int Orderid { get; set; }
        public decimal Amount { get; set; }
    }

    public class utility_helper
    {
        public void ExecuteTask()
        {
            string User_Name = "Admin";
            int itemcount = 15;

            Console.WriteLine(User_Name);

            List<int> Numbers_List = new List<int>();

            for (int i = 0; i < itemcount; i++)
            {
                Numbers_List.Add(i);
            }

            ProcessNumbers(Numbers_List);
        }

        private void ProcessNumbers(List<int> numbers)
        {
            int FinalValue = 0;

            foreach (var num in numbers)
            {
                FinalValue += num;
            }

            Console.WriteLine(FinalValue);

            FileProcessor fp = new FileProcessor();
            fp.ReadFiles();
        }
    }

    public class FileProcessor
    {
        public void ReadFiles()
        {
            List<string> FileNames = new List<string>();

            FileNames.Add("a.txt");
            FileNames.Add("b.txt");
            FileNames.Add("c.txt");

            foreach (var file in FileNames)
            {
                Console.WriteLine(file);
            }

            ConfigurationHandler handlerObj = new ConfigurationHandler();
            handlerObj.LoadSettings();
        }
    }

    public class ConfigurationHandler
    {
        private string AppVersion = "1.0";

        public void LoadSettings()
        {
            Console.WriteLine(AppVersion);

            string temp_Value = "Loaded";
            Console.WriteLine(temp_Value);

            MetricsCollector metrics = new MetricsCollector();
            metrics.Collect();
        }
    }

    public class MetricsCollector
    {
        public void Collect()
        {
            int TotalRequests = 100;
            int Failed_requests = 5;

            double SuccessRate =
                ((double)(TotalRequests - Failed_requests) / TotalRequests) * 100;

            Console.WriteLine("Success Rate: " + SuccessRate);

            LoggerClass logger = new LoggerClass();
            logger.WriteLog("Metrics collection completed");
        }
    }

    public class LoggerClass
    {
        public void WriteLog(string MessageText)
        {
            Console.WriteLine(DateTime.Now + " : " + MessageText);
        }
    }
}
//hello//
