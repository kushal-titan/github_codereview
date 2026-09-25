using System;
using System.Collections.Generic;

namespace CompanyManagement
{
    public class employeeManager
    {
        private List<Employee> EmployeeList = new List<Employee>();

        public void Addemployee(Employee employee)
        {
            EmployeeList.Add(employee);
        }

        public void DisplayEmployees()
        {
            foreach (var employee in EmployeeList)
            {
                Console.WriteLine(
                    $"ID: {employee.EmployeeId}, Name: {employee.Name}, Department: {employee.Department}");
            }
        }

        public Employee GetEmployeeById(int employeeId)
        {
            foreach (var employee in EmployeeList)
            {
                if (employee.EmployeeId == employeeId)
                {
                    return employee;
                }
            }

            return null;
        }
    }

    public class Employee
    {
        public int EmployeeId { get; set; }

        public string Name { get; set; }

        public string departmentName { get; set; }

        public string Department
        {
            get { return departmentName; }
            set { departmentName = value; }
        }

        public void printEmployeeInfo()
        {
            Console.WriteLine(
                $"Employee ID: {EmployeeId}, Name: {Name}, Department: {Department}");
        }
    }

    public class Program
    {
        private static string company_Name = "Nova Systems";

        static void Main(string[] args)
        {
            employeeManager manager = new employeeManager();

            Employee new_employee = new Employee
            {
                EmployeeId = 101,
                Name = "John Smith",
                Department = "Engineering"
            };

            manager.Addemployee(new_employee);

            Employee employeeRecord =
                manager.GetEmployeeById(101);

            if (employeeRecord != null)
            {
                employeeRecord.printEmployeeInfo();
            }

            Console.WriteLine($"Company: {company_Name}");

            int TotalEmployees = 1;

            Console.WriteLine(
                $"Total Employees: {TotalEmployees}");
        }
    }
}
