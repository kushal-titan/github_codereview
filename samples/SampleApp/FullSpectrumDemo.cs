using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace SampleApp
{
    // =========================================================================
    // Category 6: Architecture & Standards (ARCH001, ARCH003, ARCH004, ARCH005, ARCH007, ARCH008)
    // =========================================================================

    // ARCH001: Interface missing 'I' prefix
    public interface paymentProcessor
    {
        void ProcessPayment();
    }

    // ARCH003: Type naming not PascalCase (contains underscores)
    public class user_account
    {
        // ARCH004: Property naming not PascalCase
        public string first_name { get; set; } = string.Empty;
        public string last_name { get; set; } = string.Empty;
    }

    public class FullSpectrumDemo
    {
        private static readonly object _lockA = new object();
        private static readonly object _lockB = new object();

        // ARCH008: Private field PascalCase naming
        private List<string> ItemsList = new List<string>();

        // ARCH002: Async method missing 'Async' suffix
        public async Task FetchRemoteData()
        {
            await Task.Delay(10);
        }

        // ARCH007: Method missing PascalCase (starts with lowercase)
        public void printEmployeeInfo()
        {
            Console.WriteLine("Printing employee details");
        }

        // =========================================================================
        // Category 1: Structural & Complexity (CQ001, CQ002, CQ003, CQ004)
        // =========================================================================

        // CQ003: Parameter Count > 4
        public void ComplexCalculationMethod(int a, int b, int c, int d, int e)
        {
            // CQ004: Deep Nesting Depth > 3
            if (a > 0)
            {
                if (b > 0)
                {
                    if (c > 0)
                    {
                        if (d > 0)
                        {
                            Console.WriteLine("Deeply nested execution block");
                        }
                    }
                }
            }
        }

        // =========================================================================
        // Category 2: Runtime Safety & Bugs (SAF001 - SAF007)
        // =========================================================================

        public void RuntimeSafetyDemonstration(Order order, int[] numbers)
        {
            // ARCH005: Variable naming violation (snake_case)
            int item_count = 10;

            // SAF001: Deep null dereferencing without safe navigation (?.)
            string country = order.CountryCode.ToLower().Trim().ToUpper();

            // SAF002: Division by literal zero
            int brokenCalc = item_count / 0;

            // SAF003: Resource leak (IDisposable FileStream without using)
            FileStream leakStream = new FileStream("sample.dat", FileMode.OpenOrCreate);
            leakStream.WriteByte(1);

            // SAF004a: Array bounds negative index
            int negativeElem = numbers[-1];

            // SAF004b: Off-by-one loop condition
            for (int i = 0; i <= numbers.Length; i++)
            {
                Console.WriteLine(numbers[i]);
            }

            // SAF005: Empty catch block swallowing exception
            try
            {
                int val = int.Parse("bad_number");
            }
            catch (FormatException)
            {
            }

            // SAF007: Generic exception throw
            if (item_count < 0)
            {
                throw new Exception("Invalid item count");
            }

            // SAF006: Unreachable dead code
            return;
            Console.WriteLine("This statement is unreachable dead code");
        }

        // =========================================================================
        // Category 3: Concurrency & Async (CON001 - CON005)
        // =========================================================================

        // CON001: Lock Order Inversion (Lock A -> Lock B)
        public void DeadlockMethodAlpha()
        {
            lock (_lockA)
            {
                lock (_lockB)
                {
                    Console.WriteLine("Holding Lock A then Lock B");
                }
            }
        }

        // CON001: Lock Order Inversion (Lock B -> Lock A)
        public void DeadlockMethodBeta()
        {
            lock (_lockB)
            {
                lock (_lockA)
                {
                    Console.WriteLine("Holding Lock B then Lock A");
                }
            }
        }

        // CON002: async void anti-pattern
        public async void FireAndForgetCrashAsync()
        {
            await Task.Delay(100);
        }

        // CON003: Sync-over-async blocking
        public string GetResultBlocking()
        {
            return Task.FromResult("data").Result;
        }

        // CON004: Unsafe lock target (locking on 'this')
        public void UnsafeLocking()
        {
            lock (this)
            {
                Console.WriteLine("Locked on this");
            }
        }

        // CON005: Unawaited async task call
        public void TriggerBackgroundWork()
        {
            FetchRemoteData();
        }

        // =========================================================================
        // Category 4: Security & Vulnerabilities (SEC001 - SEC004)
        // =========================================================================

        public void SecurityVulnerabilityDemonstration(string userInput)
        {
            // SEC001: SQL Injection risk
            string query = $"SELECT * FROM Users WHERE Email = '{userInput}'";
            var cmd = new System.Data.SqlClient.SqlCommand();
            cmd.ExecuteReader(query);

            // SEC002: Hardcoded secret / API key
            string api_key = "AKIAIOSFODNN7EXAMPLE_SECRET_KEY_12345";

            // SEC003: Weak cryptographic algorithm
            using var md5 = MD5.Create();

            // SEC004: Cross-Site Scripting (XSS)
            var response = new DummyResponse();
            response.Write($"<div>Hello {userInput}</div>");
        }

        // =========================================================================
        // Category 5: Performance & Memory (PERF001 - PERF003)
        // =========================================================================

        public void PerformanceDemonstration(List<string> items)
        {
            string summary = "";

            // PERF001: String concatenation in loop
            for (int i = 0; i < 100; i++)
            {
                summary += "Item: " + i;
            }

            // PERF002: Boxing allocation with legacy ArrayList
            var legacyList = new System.Collections.ArrayList();
            legacyList.Add(42);

            // PERF003: LINQ .Count() > 0 instead of .Any()
            if (items.Count() > 0)
            {
                Console.WriteLine("Collection has items: " + summary);
            }
        }

        // =========================================================================
        // Category 6: Architecture & Standards (ARCH006)
        // =========================================================================

        public void ObsoleteApiDemonstration()
        {
            // ARCH006: Obsolete API usage
            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();
        }
    }

    public class DummyResponse
    {
        public void Write(string content) { }
    }
}
