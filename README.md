# Introduction

**WARNING:** this is an old repository and is only provided as a sample of how to implement `IQueryProvider`.  See the `AccessQueryProvider` class for how it works, the entry point is in `ExecuteExpression`.

The aim of this project is to create a LINQ QueryProvider for Access databases similar to Entity Framework. This is not a 3rd party Party provider for Entity Framework, it is only an example of how LINQ to SQL works.

# Features

* Generates Access-compatible SQL statements from LINQ expressions
* Supports Where, multiple Joins, First, Take, OrderBy, OrderByDescending, Select, Distinct
* Code-first approach using custom attributes to map table and field names

# Issues

* Currently only supports data retrieval (SELECT statements)
* Doesn't support self-joins
* Very buggy. Use at your own risk.

# Usage

**NOTE:** A demo project using the Northwind Access database is included.

Create a class that represents the structure of the table you wish to access. Make sure that the class name is the same as the table, and the property names and types match the columns declared for that table.

Using the Northwind database as an example:

```cs

    class Employees
    {
        public int EmployeeID { get; private set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string Title { get; set; }
        public string TitleOfCourtesy { get; set; }
        public DateTime? BirthDate { get; set; }
        public DateTime? HireDate { get; set; }
        public string Address { get; set; }
        public string Region { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public string HomePhone { get; set; }
        public string Extension { get; set; }
        public byte[]() Photo { get; set; }
        public string Notes { get; set; }
        public int? ReportsTo { get; set; }
        // for use in a drop down list...
        public override string ToString()
        {
            return LastName + ", " + FirstName; 
        }
    }
```

Alternatively, you can use the `Table` and `Column` attributes to map table and column names to class and field names:

```cs
    [Table("Employees")]
    public class Employees
    {
        [Column("ID", true)]
        public virtual int ID { get; set; }
        public virtual string Company { get; set; }
        [Column("Last Name")]
        public virtual string LastName { get; set; }
        [Column("First Name")]
        public virtual string FirstName { get; set; }
        [Column("Job Title")]
        public virtual string JobTitle { get; set; }
        [Column("E-mail Address")]
        public virtual string EmailAddress { get; set; }
        public virtual string Address { get; set; }
        public virtual string City { get; set; }
    }
```

Create a class that inherits from **AccessContext**.

Create a property for each table as a generic instance of **AccessDBSet<classname>**.

```cs
using AccessToLinq;

    class NorthWindContext : AccessContext
    {
        public NorthWindContext(string connectionString)
            : base(connectionString)
        {
        }

        public AccessDBSet<Employees> Employees { get; set; }
    }
```

Query away!

```cs
var context = new NorthWindContext("Driver={Microsoft Access Driver (*.mdb, *.accdb)}; DBQ=.\\Northwind.accdb");

var emps = context.Employees.ToList();

foreach (var emp in emps)
{
    Console.WriteLine($"{emp.LastName}, {emp.FirstName}");
}

var titles = context.Employees.Select(emp => emp.JobTitle).Distinct().OrderBy(title => title).ToList();

foreach (var title in titles)
{
    Console.WriteLine(title);
}

```