using System.Data;
using System.Data.Odbc;
using AccessToLinq;

namespace Northwind
{
    class NorthWindContext : AccessContext
    {
        public NorthWindContext(string connectionString)
            : base(connectionString)
        {
        }

        public AccessDBSet<Employees> Employees { get; set; }
    }
}