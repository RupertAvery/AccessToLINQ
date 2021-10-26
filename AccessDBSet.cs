using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace AccessToLinq
{
    public class AccessDBSet<T> : Query<T> where T : new()
    {
        private readonly AccessContext _context;

        public AccessDBSet(AccessContext context, string table)
        {
            _context = context;
            provider = new AccessQueryProvider<T>(table, context);
        }

        public AccessDBSet(AccessContext context)
        {
            _context = context;
            var table = ModelMapper.GetTableName(typeof(T));
            provider = new AccessQueryProvider<T>(table, context);
        }

        public override string ToString()
        {
            return ((AccessQueryProvider<T>)provider).GenerateQuery(this.expression);
        }

        public void Add(T entity)
        {
            var table = ModelMapper.GetTableName(typeof(T));
            var mappings = ModelMapper.GetFieldMappings(typeof(T), false);
            var sbFieldValues = new List<string>();
            var sbFieldNames = new List<string>();
            var props = entity.GetType().GetProperties();
            foreach (var prop in props)
            {
                if (mappings.ContainsKey(prop.Name))
                {
                    sbFieldNames.Add("[" + mappings[prop.Name] + "]");
                    sbFieldValues.Add(ToSQL(prop.GetValue(entity, null)));
                }
            }
            var insertSql =
                $"INSERT INTO {table} ({string.Join(", ", sbFieldNames.ToArray())}) SELECT {string.Join(", ", sbFieldValues.ToArray())}";
            var result = _context.Execute(insertSql);
            var pk = ModelMapper.GetPrimaryProperty(typeof(T));
            if (pk != null)
                pk.SetValue(entity, result, null);
        }

        protected string ToSQL(object result)
        {
            switch (result)
            {
                case null:
                    return "NULL";
                case string _:
                    return $"'{result}'";
                case DateTime _:
                    return $"#{result}#";
                default:
                    return result.ToString();
            }
        }
    }
}
