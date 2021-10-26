using System.Collections.Generic;
using System.Linq.Expressions;
using AccessToLinq;

namespace AccessToLINQ.Expressions
{
    internal class Join
    {
        private JoinTable outerObject;
        private JoinTable innerObject;
        private object outerSelector;
        private object innerSelector;
        public Dictionary<string, string> fields = new Dictionary<string, string>();


        public Join(object obj1, object obj2, object obj3, object obj4, object obj5, ref int count)
        {
            outerObject = new JoinTable(obj1, ref count);
            innerObject = new JoinTable(obj2, ref count);
            outerSelector = obj3;
            innerSelector = obj4;
            var result = obj5;

            var outer = ((Lambda)result).callexp.Parameters[0].Name;
            var inner = ((Lambda)result).callexp.Parameters[1].Name;
            Dictionary<string, RawSQL> r = (Dictionary<string, RawSQL>)((Lambda)result).p;
            foreach (var qwe in r)
            {
                if ((string)qwe.Value.Reference == outer) fields.Add(qwe.Key, outerObject.getField(qwe.Value.SQL));
                if ((string)qwe.Value.Reference == inner) fields.Add(qwe.Key, innerObject.getField(qwe.Value.SQL));
            }
        }

        internal string getField(string name)
        {
            return fields[name];
        }

        internal string getJoin()
        {
            var p = ModelMapper.GetTableName(((ConstantExpression)innerObject.obj1).Type.GetGenericArguments()[0]);

            return string.Format("INNER JOIN {0} {3} ON {1} = {2}) ", p, innerObject.Alias + "." + ((RawSQL)((Lambda)innerSelector).p).SQL, outerObject.getField(((RawSQL)((Lambda)outerSelector).p).SQL), innerObject.Alias);
        }

        internal bool hasField(string p)
        {
            return fields.ContainsKey(p);
        }
    }
}