using System;

namespace AccessToLinq
{
    public static class TypeExtensions
    {
        public static bool IsRuntimeType(this Type type)
        {
            return type.UnderlyingSystemType.Name == "RuntimeType";
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class Column : Attribute
    {
        public string field { get; set; }
        public bool primarykey { get; set; }

        public Column(string field)
        {
            this.field = field;
        }

        public Column(string field, bool primarykey)
        {
            this.field = field;
            this.primarykey = primarykey;
        }

    }

    [AttributeUsage(AttributeTargets.Class)]
    public class Table : Attribute
    {
        public string table { get; set; }

        public Table(string table)
        {
            this.table = table;
        }

    }

}
