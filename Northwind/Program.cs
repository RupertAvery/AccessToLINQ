using System;
using System.Linq;
using AccessToLinq;

namespace Northwind
{
    class Program
    {
        static void Main(string[] args)
        {
            var context = new NorthWindContext("Driver={Microsoft Access Driver (*.mdb, *.accdb)}; DBQ=.\\Northwind.accdb");

            //var proxy = new Employees_Proxy();
            //proxy.JobTitle = "Hello!";

            //var emoployeeProxyType = ProxyTypeBuilder.GetOrCreateProxyType(typeof(Employees));
            //Employees emp2 = (Employees)Activator.CreateInstance(emoployeeProxyType);

            //emp2.JobTitle = "Hello!";

            //var dirty = ((IProxy)emp2).GetDirtyProperties();

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

            Console.ReadLine();
        }
    }
}
