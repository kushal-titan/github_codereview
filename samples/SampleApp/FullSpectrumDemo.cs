using System;

namespace DivisionErrorsDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            int totalMarks = 500;
            int students = 0;

            // Divide by zero error #1
            int averageMarks = totalMarks / students;
            Console.WriteLine("Average Marks: " + averageMarks);

            int profit = 1000;
            int months = 0;

            // Divide by zero error #2
            int monthlyProfit = profit / months;
            Console.WriteLine("Monthly Profit: " + monthlyProfit);

            Console.WriteLine("Program Completed");
        }
    }
}
