using System;
using AccessToLinq;

namespace Northwind
{
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
}