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

            // ❌ Error: SAF002 Division by Zero
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
            int count = 5;

            // ✅ Safe Calculation (Resolved)
            int average = count != 0 ? total / count : 0;
            Console.WriteLine(average);
        }

        static void CalculatePercentage()
        {
            int obtained = 75;
            int maximum = 100;

            // ✅ Safe Calculation
            int percentage = maximum != 0 ? (obtained * 100) / maximum : 0;
            Console.WriteLine(percentage);
        }

        static void ProcessData()
        {
            int records = 250;
            int groups = 10;

            // ✅ Safe Calculation
            int perGroup = groups != 0 ? records / groups : 0;
            Console.WriteLine(perGroup);

            MoreProcessing();
        }

        static void MoreProcessing()
        {
            int value = 900;
            int divisor = 3;

            // ✅ Safe Calculation
            int output = divisor != 0 ? value / divisor : 0;
            Console.WriteLine(output);
        }

        static void ComputeRatio()
        {
            int x = 40;
            int y = 4;

            // ✅ Safe Calculation
            int ratio = y != 0 ? x / y : 0;
            Console.WriteLine(ratio);

            NestedCalculation();
        }

        static void NestedCalculation()
        {
            int numerator = 1000;
            int denominator = 10;

            // ✅ Safe Calculation
            int result = denominator != 0 ? numerator / denominator : 0;
            Console.WriteLine(result);

            FinalCalculation();
        }

        static void FinalCalculation()
        {
            int sales = 5000;
            int months = 12;

            // ✅ Safe Calculation
            int monthlySales = months != 0 ? sales / months : 0;
            Console.WriteLine(monthlySales);

            int distance = 100;
            int time = 2;

            // ✅ Safe Calculation
            int speed = time != 0 ? distance / time : 0;
            Console.WriteLine(speed);
        }
    }
}
