### Usage

Create a class that represents the structure of the table you wish to access. Make sure that the class name is the same as the table, and the property names and types match the columns declared for that table.

Using the Northwind database as an example:

{code:C#}

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

{code:C#}

Alternatively, you can use custom attributes to map table and column names to class and field names:

{code:C#}

    [DBTableMapping("users")](DBTableMapping(_users_))
    public class User
    {
        // primarykey = true (no real use yet)
        [DBFieldMapping("user_id", true)](DBFieldMapping(_user_id_,-true))
        public int Id { get; set; }
        [DBFieldMapping("user_name")](DBFieldMapping(_user_name_))
        public string Name { get; set; }
        public override string ToString()
        {
            return Name;
        }
    }

{code:C#}

Create a class that inherits from **ACCDBContext**. Override **CreateConnection** to create a connection of the desired type, using the connection string passed in the constructor.

Create a property for each table as a generic instance of **ACCDBSet<classname>**.

{code:C#}
using System;
using AccessToLinq;
using System.Data;
using System.Data.Odbc;

    class NorthWindContext : ACCDBContext
    {
        public NorthWindContext(string connectionString)
            : base(connectionString)
        {
        }

        public override IDbConnection CreateConnection()
        {
            IDbConnection connection = new OdbcConnection();
            connection.ConnectionString = this.connectionString;
            return connection;
        }

        public ACCDBSet<Employees> Employees
        {
            get
            {
                return new ACCDBSet<Employees>(this);
            }
        }

    }

{code:C#}

Query away!

{code:C#}
        NorthWindContext context = new NorthWindContext(@"Driver={Microsoft Access Driver (**.mdb, **.accdb)}; DBQ=.\Nwind.mdb");

        private void Form1_Load(object sender, EventArgs e)
        {
            var titles = context.Employees.Select(x => x.Title).Distinct().OrderBy(y => y);

            foreach (var title in titles)
            {
                comboBox2.Items.Add(title);
            }

        }
{code:C#}