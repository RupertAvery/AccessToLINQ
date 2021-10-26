using AccessToLinq;

namespace Northwind
{
    [Table("users")]
    public class User
    {
        // primarykey = true (no real use yet)
        [Column("user_id", true)]
        public int Id { get; set; }
        [Column("user_name")]
        public string Name { get; set; }
        public override string ToString()
        {
            return Name;
        }
    }
}