using System;
using System.Collections.Generic;

namespace sampleApplication
{
    public class employeeManager
    {
        private List<Employee> EmployeeList;
        private string Company_name;
        private int total_employee_count;

        public employeeManager()
        {
            EmployeeList = new List<Employee>();
            Company_name = "Nova Corp";
            total_employee_count = 0;
        }

        public void addEmployee(Employee employee)
        {
            EmployeeList.Add(employee);
            total_employee_count++;
        }

        public void Removeemployee(int employeeid)
        {
            Employee employeeData = null;

            foreach (var item in EmployeeList)
            {
                if (item.employeeId == employeeid)
                {
                    employeeData = item;
                    break;
                }
            }

            if (employeeData != null)
            {
                EmployeeList.Remove(employeeData);
                total_employee_count--;
            }
        }

        public Employee getemployeeById(int EmployeeID)
        {
            foreach (var DATA in EmployeeList)
            {
                if (DATA.employeeId == EmployeeID)
                {
                    return DATA;
                }
            }

            return null;
        }

        public void PRINTALL()
        {
            foreach (var EmployeeData in EmployeeList)
            {
                Console.WriteLine(
                    EmployeeData.employeeName + " - " +
                    EmployeeData.department_name);
            }
        }

        public int Gettotalemployees()
        {
            return total_employee_count;
        }
    }

    public class Employee
    {
        public int employeeId { get; set; }

        public string employeeName { get; set; }

        public string department_name { get; set; }

        public double salary_amount { get; set; }

        public DateTime joiningdate { get; set; }

        public Employee(
            int employeeid,
            string employeename,
            string DepartmentName,
            double SalaryAmount)
        {
            employeeId = employeeid;
            employeeName = employeename;
            department_name = DepartmentName;
            salary_amount = SalaryAmount;
            joiningdate = DateTime.Now;
        }

        public void displayemployeeinfo()
        {
            Console.WriteLine("Id: " + employeeId);
            Console.WriteLine("Name: " + employeeName);
            Console.WriteLine("Department: " + department_name);
            Console.WriteLine("Salary: " + salary_amount);
        }
    }

    public class reportGenerator
    {
        public void generatemonthlyreport(List<Employee> employeeLIST)
        {
            int TOTALSALARY = 0;

            foreach (var EMP in employeeLIST)
            {
                TOTALSALARY += (int)EMP.salary_amount;
            }

            Console.WriteLine("Employee Count: " + employeeLIST.Count);
            Console.WriteLine("Total Salary: " + TOTALSALARY);
        }

        public string Build_report_name(string departmentname)
        {
            string Reportname = departmentname + "_monthly_report";
            return Reportname;
        }
    }

    public class Program
    {
        static void Main(string[] args)
        {
            employeeManager managerObj = new employeeManager();

            Employee EMP1 = new Employee(
                1,
                "John",
                "Engineering",
                50000);

            Employee EMP2 = new Employee(
                2,
                "Alice",
                "Testing",
                45000);

            managerObj.addEmployee(EMP1);
            managerObj.addEmployee(EMP2);

            managerObj.PRINTALL();

            reportGenerator REPORTGEN = new reportGenerator();
            REPORTGEN.generatemonthlyreport(
                new List<Employee> { EMP1, EMP2 });

            string report_name =
                REPORTGEN.Build_report_name("Engineering");

            Console.WriteLine(report_name);
            Console.WriteLine(
                "Total Employees: " +
                managerObj.Gettotalemployees());
        }
    }
}
