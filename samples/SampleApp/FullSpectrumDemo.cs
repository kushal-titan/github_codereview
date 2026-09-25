public class employeeManager       
{
    private string EmployeeName;

    private int employee_age;      

    public void calculateSalary()  
    {
        int TotalSalary = 50000;   
        Console.WriteLine(TotalSalary);
    }

    public void ProcessData(int User_ID) 
    {
        Console.WriteLine(User_ID);
    }

    public string GetemployeeName() 
    {
        return EmployeeName;
    }

    public string department_name   
    {
        get;
        set;
    }

    public const string company_name = "Nova"; 

    public event EventHandler employeeAdded;   
}
