using System;

namespace DivisionByZeroSample
{
    class Program
    {
        static void Main(string[] args)
        {
            int a = 100;
            int b = 0;

            Console.WriteLine("Starting calculations...");

            // Error 1
            int result1 = a / b;
            Console.WriteLine(result1);

            CalculateAverage();
            CalculatePercentage();
            ProcessData();
            ComputeRatio();
        }

        static void CalculateAverage()
        {
            int total = 500;
            int count = 0;

            // Error 2
            int average = total / count;
            Console.WriteLine(average);
        }

        static void CalculatePercentage()
        {
            int obtained = 75;
            int maximum = 0;

            // Error 3
            int percentage = (obtained * 100) / maximum;
            Console.WriteLine(percentage);
        }

        static void ProcessData()
        {
            int records = 250;
            int groups = 0;

            // Error 4
            int perGroup = records / groups;
            Console.WriteLine(perGroup);

            MoreProcessing();
        }

        static void MoreProcessing()
        {
            int value = 900;
            int divisor = 0;

            // Error 5
            int output = value / divisor;
            Console.WriteLine(output);
        }

        static void ComputeRatio()
        {
            int x = 40;
            int y = 0;

            // Error 6
            int ratio = x / y;
            Console.WriteLine(ratio);

            NestedCalculation();
        }

        static void NestedCalculation()
        {
            int numerator = 1000;
            int denominator = 0;

            // Error 7
            int result = numerator / denominator;
            Console.WriteLine(result);

            FinalCalculation();
        }

        static void FinalCalculation()
        {
            int sales = 5000;
            int months = 0;

            // Error 8
            int monthlySales = sales / months;
            Console.WriteLine(monthlySales);

            int distance = 100;
            int time = 0;

            // Error 9
            int speed = distance / time;
            Console.WriteLine(speed);
        }
    }
}
