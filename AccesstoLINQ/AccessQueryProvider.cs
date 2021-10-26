using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using System.Data;
using System.Reflection;
using System.Linq.Expressions;
using AccessToLINQ.Expressions;

namespace AccessToLinq
{
    class AccessQueryProvider<T> : QueryProvider where T : new()
    {
        private string _joinClause;
        private string _topClause;
        private string _whereClause;
        private string _orderByClause;
        private string _groupByClause;
        private readonly string _table;
        private bool _hasFirst;
        private bool _hasOrderBy;
        private bool _hasGroupBy;
        private int _tableno = 0;
        private int _tablecount = 0;
        private Type _lastJoinType;
        private Join _lastJoin;

        private readonly AccessContext _context;
        private Dictionary<Type, Dictionary<string, RawSQL>> _joinTableTypeFields = new Dictionary<Type, Dictionary<string, RawSQL>>();
        private Dictionary<Type, string> _joinTableTypeTables = new Dictionary<Type, string>();
        private Stack<string> _memberaccessstack = new Stack<string>();

        public AccessQueryProvider(string table, AccessContext context)
        {
            _table = table;
            _context = context;
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
            }

            if ((_lastJoin != null) && (result.GetType() == typeof(RawSQL)) && (((RawSQL)result).Reference != null)) return _lastJoin.getField(result.ToString());

            return result.ToString();
        }

        protected object Evaluate(Expression expression)
        {
            object returnval = null;
            Dictionary<string, RawSQL> fields;
            switch (expression.NodeType)
            {
                case ExpressionType.Parameter:
                    returnval = expression.Type;
                    break;
                case ExpressionType.AndAlso:
                case ExpressionType.And:
                    {
                        BinaryExpression binexp = ((BinaryExpression)expression);
                        returnval = new RawSQL("(" + ToSQL(Evaluate(binexp.Left)) + ") AND (" + ToSQL(Evaluate(binexp.Right)) + ")");
                    }
                    break;

                case ExpressionType.OrElse:
                case ExpressionType.Or:
                    {
                        BinaryExpression binexp = ((BinaryExpression)expression);
                        returnval = new RawSQL("(" + ToSQL(Evaluate(binexp.Left)) + ") OR (" + ToSQL(Evaluate(binexp.Right)) + ")");
                    }
                    break;

                case ExpressionType.Add:
                    {
                        BinaryExpression binexp = ((BinaryExpression)expression);
                        returnval = new RawSQL(ToSQL(Evaluate(binexp.Left)) + " + " + ToSQL(Evaluate(binexp.Right)));
                    }
                    break;


                case ExpressionType.Equal:
                    {
                        BinaryExpression binexp = ((BinaryExpression)expression);
                        returnval = new RawSQL(ToSQL(Evaluate(binexp.Left)) + " = " + ToSQL(Evaluate(binexp.Right)));
                    }
                    break;

                case ExpressionType.NotEqual:
                    {
                        BinaryExpression binexp = ((BinaryExpression)expression);
                        returnval = new RawSQL(ToSQL(Evaluate(binexp.Left)) + " <> " + ToSQL(Evaluate(binexp.Right)));
                    }
                    break;

                case ExpressionType.LessThan:
                    {
                        BinaryExpression binexp = ((BinaryExpression)expression);
                        returnval = new RawSQL(ToSQL(Evaluate(binexp.Left)) + " < " + ToSQL(Evaluate(binexp.Right)));
                    }
                    break;

                case ExpressionType.LessThanOrEqual:
                    {
                        BinaryExpression binexp = ((BinaryExpression)expression);
                        returnval = new RawSQL(ToSQL(Evaluate(binexp.Left)) + " <= " + ToSQL(Evaluate(binexp.Right)));
                    }
                    break;

                case ExpressionType.GreaterThan:
                    {
                        BinaryExpression binexp = ((BinaryExpression)expression);
                        returnval = new RawSQL(ToSQL(Evaluate(binexp.Left)) + " > " + ToSQL(Evaluate(binexp.Right)));
                    }
                    break;

                case ExpressionType.GreaterThanOrEqual:
                    {
                        BinaryExpression binexp = ((BinaryExpression)expression);
                        returnval = new RawSQL(ToSQL(Evaluate(binexp.Left)) + " >= " + ToSQL(Evaluate(binexp.Right)));
                    }
                    break;

                case ExpressionType.MemberInit:
                    MemberInitExpression miexp = (MemberInitExpression)expression;
                    fields = new Dictionary<string, RawSQL>();
                    foreach (MemberBinding binding in miexp.Bindings)
                    {
                        fields.Add(binding.Member.Name, (RawSQL)Evaluate(((MemberAssignment)binding).Expression));
                    }
                    returnval = fields;
                    break;

                case ExpressionType.New:
                    NewExpression newexp = (NewExpression)expression;
                    fields = new Dictionary<string, RawSQL>();
                    if (newexp.Arguments[0].NodeType == ExpressionType.MemberAccess)
                    {
                        if (newexp.Arguments.Count > 0)
                        {

                            for (int i = 0; i < newexp.Arguments.Count; i++)
                            {
                                fields.Add(newexp.Members[i].Name, (RawSQL)Evaluate(newexp.Arguments[i]));
                            }

                        }
                    }
                    else
                    {
                        for (int i = 1; i < newexp.Arguments.Count; i++)
                        {
                            Type t = (Type)Evaluate(newexp.Arguments[i]);
                            foreach (PropertyInfo pi in t.GetProperties())
                            {
                                fields.Add(pi.Name, new RawSQL(ModelMapper.GetTableName(t), ModelMapper.GetFieldName(t, pi.Name)));
                            }
                        }
                    }
                    returnval = fields;
                    break;

                case ExpressionType.Quote:
                    {
                        LambdaExpression callexp = (LambdaExpression)((UnaryExpression)expression).Operand;
                        returnval = new Lambda(callexp, Evaluate(callexp.Body));
                    }
                    break;

                case ExpressionType.MemberAccess:
                    {
                        MemberExpression propexp = (MemberExpression)expression;
                        string propertyName = propexp.Member.Name;

                        if (propexp.Expression != null)
                        {
                            if (propexp.Expression.Type.GetType().IsRuntimeType())
                            {
                                // If this is a runtime type, then we probably need to convert it into an SQL statement
                                if (propexp.Expression.NodeType == ExpressionType.Call)
                                {
                                    object result = Evaluate(propexp.Expression);

                                    if (propexp.Expression.Type == typeof(TimeSpan))
                                    {
                                        string timespan = "";
                                        switch (propertyName)
                                        {
                                            case "Seconds": timespan = "s"; break;
                                            case "Days": timespan = "d"; break;
                                            case "Hours": timespan = "h"; break;
                                            case "Months": timespan = "m"; break;
                                            case "Years": timespan = "y"; break;
                                        }
                                        returnval = new RawSQL(string.Format(result.ToString(), timespan));
                                    }
                                    else
                                    {
                                        returnval = result;
                                    }
                                }
                                else if (propexp.Expression.NodeType == ExpressionType.Parameter)
                                {
                                    //if (((ParameterExpression)propexp.Expression).Name.Contains("Transparent"))
                                    //{
                                    //    returnval = propexp.Member;

                                    //}
                                    //else
                                    //{
                                    returnval = new RawSQL(((ParameterExpression)propexp.Expression).Name, ModelMapper.GetFieldName(propexp.Expression.Type, propertyName));
                                    //}
                                }
                                else if (propexp.Expression.NodeType == ExpressionType.MemberAccess)
                                {
                                    object obj = Evaluate(propexp.Expression);
                                    //if (obj.GetType().BaseType==typeof(PropertyInfo))
                                    //{
                                    //    return new RawSQL(
                                    //        ((ParameterExpression)((MemberExpression)propexp.Expression).Expression).Name, 
                                    //        ModelMapper.GetFieldName(((PropertyInfo)obj).PropertyType, propertyName));
                                    //}
                                    if (propexp.Expression.Type.IsGenericType && propexp.Expression.Type.GetGenericTypeDefinition() == typeof(Nullable<>))
                                    {
                                        returnval = obj;
                                    }
                                    else
                                    {
                                        //returnval = Traverse(propexp.Expression);
                                        Type type = null;

                                        if (propexp.Member.MemberType == MemberTypes.Field)
                                        {
                                            FieldInfo field = obj.GetType().GetField(propexp.Member.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.GetField | BindingFlags.FlattenHierarchy);
                                            if (field != null)
                                            {
                                                returnval = field.GetValue(obj);
                                                type = field.FieldType;
                                            }
                                            else
                                            {
                                                throw new MemberAccessException(
                                                    $"Member '{propexp.Member.Name}' was not found on type '{obj.GetType().Name}'");
                                            };

                                        }
                                        else if (propexp.Member.MemberType == MemberTypes.Property)
                                        {
                                            PropertyInfo prop = obj.GetType().GetProperty(propexp.Member.Name);
                                            if (prop != null)
                                            {
                                                returnval = prop.GetValue(obj, null);
                                                type = prop.PropertyType;
                                            }
                                            else
                                            {
                                                throw new MemberAccessException(
                                                    $"Member '{propexp.Member.Name}' was not found on type '{obj.GetType().Name}'");
                                            };
                                        }
                                    }
                                }
                                else if (propexp.Expression.NodeType == ExpressionType.Constant)
                                {
                                    Type type = null;
                                    var obj = Evaluate(propexp.Expression);
                                    if (propexp.Member.MemberType == MemberTypes.Field)
                                    {
                                        var field = obj.GetType().GetField(propexp.Member.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.GetField | BindingFlags.FlattenHierarchy);
                                        if (field != null)
                                        {
                                            returnval = field.GetValue(obj);
                                            type = field.FieldType;
                                        }
                                        else
                                        {
                                            throw new MemberAccessException(
                                                $"Member '{propexp.Member.Name}' was not found on type '{obj.GetType().Name}'");
                                        };

                                    }
                                    else if (propexp.Member.MemberType == MemberTypes.Property)
                                    {
                                        var prop = obj.GetType().GetProperty(propexp.Member.Name);
                                        if (prop != null)
                                        {
                                            returnval = prop.GetValue(obj, null);
                                            type = prop.PropertyType;
                                        }
                                        else
                                        {
                                            throw new MemberAccessException(
                                                $"Member '{propexp.Member.Name}' was not found on type '{obj.GetType().Name}'");
                                        };
                                    }
                                }
                                else
                                {
                                    throw new NotSupportedException();
                                }
                            }
                            else
                            {
                                Type type = null;

                                var obj = Evaluate(propexp.Expression);

                                if (propexp.Member.MemberType == MemberTypes.Field)
                                {
                                    var field = obj.GetType().GetField(propexp.Member.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.GetField | BindingFlags.FlattenHierarchy);
                                    if (field != null)
                                    {
                                        returnval = field.GetValue(obj);
                                        type = field.GetType();
                                    }
                                    else
                                    {
                                        throw new MemberAccessException(
                                            $"Member '{propexp.Member.Name}' was not found on type '{obj.GetType().Name}'");
                                    };

                                }
                                else if (propexp.Member.MemberType == MemberTypes.Property)
                                {
                                    var prop = obj.GetType().GetProperty(propexp.Member.Name);
                                    if (prop != null)
                                    {
                                        returnval = prop.GetValue(obj, null);
                                        type = prop.GetType();
                                    }
                                    else
                                    {
                                        throw new MemberAccessException(
                                            $"Member '{propexp.Member.Name}' was not found on type '{obj.GetType().Name}'");
                                    };
                                }
                                if (type == typeof(string)) returnval = $"'{returnval}'";
                                if (type == typeof(DateTime)) returnval = $"#{returnval}#";

                            }
                        }
                        else
                        {
                            // If we have a null expression then we are accessing a Type, not an object
                            if (propexp.Type == typeof(DateTime))
                            {
                                if (propexp.Member.Name == "Now")
                                    returnval = "DATE()";
                                else
                                    throw new NotSupportedException();
                            }
                            else
                            {
                                throw new NotSupportedException();
                            }
                        }

                    }
                    break;
                case ExpressionType.Constant:

                    var constexp = ((ConstantExpression)expression);
                    if (constexp.Type.IsGenericType)
                    {
                        var p = constexp.Type.GetGenericArguments()[0];
                        //returnval = new RawSQL(ModelMapper.GetTableName(p));
                        returnval = constexp;
                    }
                    else
                    {
                        return constexp.Value;
                    }
                    break;

                case ExpressionType.Call:
                    {
                        var callexp = (MethodCallExpression)expression;

                        if (callexp.Type == typeof(String))
                        {
                            switch (callexp.Method.Name)
                            {
                                case "ToString":
                                    returnval = Evaluate(callexp.Object);
                                    break;
                            }
                        }
                        else if (typeof(IQueryable).IsAssignableFrom(callexp.Arguments[0].Type))
                        {
                            // LINQ Method calls
                            switch (callexp.Method.Name)
                            {
                                case "First":
                                case "FirstOrDefault":
                                    _topClause = "1";
                                    Evaluate(callexp.Arguments[0]);
                                    _hasFirst = true;
                                    break;
                                case "Distinct":
                                    Evaluate(callexp.Arguments[0]);
                                    hasDistinct = true;
                                    break;

                                case "OrderBy":
                                    Evaluate(callexp.Arguments[0]);
                                    if (selectExpression != null)
                                    {
                                        _orderByClause = GetSelectFields(false);
                                    }
                                    else
                                    {
                                        _orderByClause = ((RawSQL)((Lambda)Evaluate(callexp.Arguments[1])).p).ToString();
                                    }
                                    _hasOrderBy = true;
                                    break;


                                case "GroupBy":
                                    Evaluate(callexp.Arguments[0]);
                                    if (selectExpression != null)
                                    {
                                        throw new NotImplementedException();
                                        //groupByClause = getSelectFields(false);
                                    }
                                    else
                                    {
                                        _groupByClause = ((RawSQL)((Lambda)Evaluate(callexp.Arguments[1])).p).ToString();
                                    }
                                    _hasGroupBy = true;
                                    break;

                                case "OrderByDescending":
                                    object r = Evaluate(callexp.Arguments[0]);
                                    if (r.GetType() == typeof(Join))
                                        _orderByClause = ((Join)r).getField(((RawSQL)((Lambda)Evaluate(callexp.Arguments[1])).p).ToString()) + " DESC ";
                                    else
                                        _orderByClause = ((RawSQL)((Lambda)Evaluate(callexp.Arguments[1])).p).ToString() + " DESC ";
                                    _hasOrderBy = true;

                                    break;

                                case "Count":
                                    Evaluate(callexp.Arguments[0]);
                                    hasCount = true;
                                    break;



                                case "Take":
                                    _topClause = ToSQL(Evaluate(callexp.Arguments[1]));
                                    Evaluate(callexp.Arguments[0]);
                                    break;

                                case "Join":
                                    Join j = new Join(Evaluate(callexp.Arguments[0]),
                                                    Evaluate(callexp.Arguments[1]),
                                                    Evaluate(callexp.Arguments[2]),
                                                    Evaluate(callexp.Arguments[3]),
                                                    Evaluate(callexp.Arguments[4]), ref _tablecount);

                                    _joinClause += j.getJoin();

                                    joinFields = j.fields.Select(k => k.Value + " AS [" + k.Key + "]");

                                    _lastJoinType = ((LambdaExpression)((UnaryExpression)callexp.Arguments[4]).Operand).ReturnType;

                                    _lastJoin = j;

                                    returnval = j;
                                    break;

                                case "Select":
                                    Evaluate(callexp.Arguments[0]);
                                    //hasSelect = true;
                                    selectExpression = ((UnaryExpression)callexp.Arguments[1]).Operand;
                                    break;

                                case "Where":
                                    wheredepth++;
                                    string query = string.Empty;
                                    object ret = Evaluate(callexp.Arguments[0]);

                                    for (int i = 1; i < callexp.Arguments.Count; i++)
                                    {
                                        var clause = callexp.Arguments[i];
                                        object result = Evaluate(clause);

                                        if ((result != null) && (clause.NodeType != ExpressionType.Constant))
                                        {
                                            if (clause.NodeType == ExpressionType.Quote)
                                            {
                                                //if (typeof(Join) == ret.GetType())
                                                //{
                                                //query += " AND " + ((Join)ret).getField(Evaluate(clause).ToString());
                                                query += " AND " + ((RawSQL)((Lambda)result).p).ToString();
                                                //}
                                            }
                                            else
                                            {
                                                query = ((RawSQL)((Lambda)result).p).ToString();
                                            }
                                        }
                                    }
                                    wheredepth--;

                                    if (wheredepth == 0 && query.Length > 5)
                                        _whereClause = query.Remove(0, 5) + " " + _whereClause;
                                    else
                                        _whereClause = query + " " + _whereClause;

                                    returnval = ret;
                                    break;

                                default:
                                    throw new Exception($"Unsupported LINQ method call: '{callexp.Method.Name}'");
                            }
                        }
                        else
                        {
                            if (callexp.Object != null)
                            {

                                List<object> arguments = new List<object>();
                                foreach (var args in callexp.Arguments)
                                {
                                    arguments.Add(Evaluate(args));
                                }

                                string le = (string)Evaluate(callexp.Object);
                                switch (callexp.Object.Type.Name)
                                {
                                    case "DateTime":
                                        switch (callexp.Method.Name)
                                        {
                                            case "Subtract":
                                                returnval = new RawSQL("DATEDIFF('{0}', " + _lastJoin.getField(arguments[0].ToString()) + ", " + le + ")");
                                                break;
                                            case "Add":
                                                returnval = new RawSQL("DATEADD('{0}', " + _lastJoin.getField(arguments[0].ToString()) + ", " + le + ")");
                                                break;

                                            default:
                                                throw new Exception($"Unsupported method call: '{callexp.Method.Name}'");
                                        }
                                        break;

                                    default:
                                        throw new Exception($"Unsupported type: '{callexp.Object.Type.Name}'");

                                }
                            }
                            else
                            {

                                var arguments = new List<object>();
                                foreach (var args in callexp.Arguments)
                                {
                                    arguments.Add(Evaluate(args));
                                }

                                returnval = callexp.Method.Invoke(null, arguments.ToArray());


                            }
                        }
                    }
                    break;

                case ExpressionType.Convert:
                    returnval = Evaluate(((UnaryExpression)expression).Operand);
                    break;

                default:
                    throw new Exception(
                        $"Unsupported type: '{Enum.GetName(typeof(ExpressionType), expression.NodeType)}'");
            }
            return returnval;
        }

        int wheredepth = 0;
        private Expression selectExpression;
        private bool hasCount;
        private bool hasDistinct;
        private IEnumerable<string> joinFields;

        //CachedQueryProvider cqp = new CachedQueryProvider();

        internal string GenerateQuery(Expression expression)
        {
            Evaluate(expression);

            List<string> fields = ModelMapper.GetFields(typeof(T));

            return string.Format("SELECT " +
                                 (hasDistinct ? " DISTINCT " : "") +
                                 (_topClause != null ? " TOP " + _topClause + " " : "") +
                                 (selectExpression != null ? GetSelectFields() :
                                     (_joinClause != null ?
                                         string.Join(",", joinFields)
                                         :
                                         "[t0." + string.Join("], [t0.", fields.ToArray()) + "] "
                                     )) +
                                 " FROM " + (_tablecount - 1 > 0 ? "".PadLeft(_tablecount - 1, '(') : "") + _table + " t0 " +
                                 (_joinClause ?? "") +
                                 (_whereClause != null ? " WHERE " + _whereClause : "") +
                                 (_orderByClause != null ? " ORDER BY " + _orderByClause : "") +
                                 (_groupByClause != null ? " GROUP BY " + _groupByClause : "")
            );
        }

        //public override string ToString()
        //{
        //    return GenerateQuery;
        //}

        protected override object ExecuteExpression(Expression expression)
        {
            object returnval = null;

            var sqlcommand = GenerateQuery(expression);

            using (var conn = _context.CreateConnection())
            {

                System.Diagnostics.Debug.WriteLine(sqlcommand);

                conn.Open();
                var comm = conn.CreateCommand();
                comm.CommandText = sqlcommand;
                comm.CommandType = CommandType.Text;

                var rdr = comm.ExecuteReader();

                if (selectExpression != null)
                {
                    Type selectType = ((LambdaExpression)selectExpression).ReturnType;

                    var outputgenerictype = typeof(List<>).MakeGenericType(selectType);
                    object iitems = Activator.CreateInstance(outputgenerictype);
                    ConstructorInfo constructor = null;
                    ParameterInfo[] parameterInfoes = null;
                    bool isAnonymousType = selectType.IsAnonymousType();
                    MethodInfo mListAdd = outputgenerictype.GetMethod("Add");

                    if (isAnonymousType)
                    {
                        constructor = selectType.GetConstructors()[0];
                        parameterInfoes = constructor.GetParameters();
                    }

                    while (rdr.Read())
                    {
                        var paramlist = new List<object>();
                        object obj = null;
                        // Anonymous types have a parameterized constructor
                        if (isAnonymousType)
                        {
                            // Create the parameter array by mapping the joined field names to the constructor parameter names
                            foreach (ParameterInfo parameterInfo in parameterInfoes)
                            {
                                string mappedfield = parameterInfo.Name;
                                paramlist.Add(ModelMapper.GetValue(rdr, mappedfield, parameterInfo.ParameterType));
                            }
                            obj = constructor.Invoke(paramlist.ToArray());

                        }
                        else
                        {
                            if (selectType.GetType().IsRuntimeType())
                            {
                                object result = Evaluate(((LambdaExpression)selectExpression).Body);
                                if (result.GetType() == typeof(Dictionary<string, RawSQL>))
                                {
                                    Dictionary<string, RawSQL> r = (Dictionary<string, RawSQL>)result;
                                    obj = Activator.CreateInstance(selectType);
                                    foreach (PropertyInfo pi in selectType.GetProperties())
                                    {
                                        if (r.ContainsKey(pi.Name))
                                        {
                                            string mappedfield = r[pi.Name].SQL;
                                            object getval = ModelMapper.GetValue(rdr, mappedfield, pi.PropertyType);
                                            pi.SetValue(obj, getval, null);
                                        }
                                    }
                                }
                                else
                                {
                                    string fullname = ((RawSQL)result).ToString();
                                    string mappedfield = fullname.Substring(fullname.IndexOf(".") + 1);
                                    obj = ModelMapper.GetValue(rdr, mappedfield, selectType);

                                }


                            }
                            else
                            {
                                obj = Activator.CreateInstance(selectType);
                                foreach (PropertyInfo pi in selectType.GetProperties())
                                {
                                    string mappedfield = pi.Name;
                                    pi.SetValue(obj, ModelMapper.GetValue(rdr, mappedfield, pi.PropertyType), null);
                                }
                            }

                        }
                        mListAdd.Invoke(iitems, new object[] { obj });
                    }

                    returnval = iitems;

                }
                else if (_joinClause != null)
                {
                    // Create a 

                    var outputgenerictype = typeof(List<>).MakeGenericType(_lastJoinType);
                    object iitems = Activator.CreateInstance(outputgenerictype);
                    ConstructorInfo constructor = null;
                    ParameterInfo[] parameterInfoes = null;
                    bool isAnonymousType = _lastJoinType.IsAnonymousType();
                    MethodInfo mListAdd = outputgenerictype.GetMethod("Add");

                    if (isAnonymousType)
                    {
                        constructor = _lastJoinType.GetConstructors()[0];
                        parameterInfoes = constructor.GetParameters();
                    }

                    while (rdr.Read())
                    {
                        var paramlist = new List<object>();
                        object obj;
                        // Anonymous types have a parameterized constructor
                        if (isAnonymousType)
                        {
                            // Create the parameter array by mapping the joined field names to the constructor parameter names
                            foreach (ParameterInfo parameterInfo in parameterInfoes)
                            {
                                string mappedfield = parameterInfo.Name;
                                paramlist.Add(ModelMapper.GetValue(rdr, mappedfield, parameterInfo.ParameterType));
                            }
                            obj = constructor.Invoke(paramlist.ToArray());

                        }
                        else
                        {
                            obj = Activator.CreateInstance(_lastJoinType);
                            foreach (PropertyInfo pi in _lastJoinType.GetProperties())
                            {
                                if (_lastJoin.hasField(pi.Name))
                                {
                                    pi.SetValue(obj, ModelMapper.GetValue(rdr, pi.Name, pi.PropertyType), null);
                                }
                            }

                        }
                        mListAdd.Invoke(iitems, new object[] { obj });
                    }

                    returnval = GetReturnVal(iitems);
                }
                else
                {
                    var items = new List<T>();
                    while (rdr.Read())
                    {
                        //T obj = new T();
                        var proxyType = ProxyTypeBuilder.GetOrCreateProxyType(typeof(T));
                        T obj = (T)Activator.CreateInstance(proxyType);
                        ModelMapper.Map(typeof(T), rdr, obj);
                        proxyType.GetMethod("EnableTracking").Invoke(obj, null);
                        items.Add(obj);
                    }
                    returnval = GetReturnVal(items);
                }

                conn.Close();
            }
            return returnval;
        }


        private string GetSelectFields()
        {
            return GetSelectFields(true);
        }

        private string GetSelectFields(bool includeAlias)
        {
            List<string> fieldList = new List<string>(); ;
            if (selectExpression != null)
            {
                Type selectType = ((LambdaExpression)selectExpression).ReturnType;

                ConstructorInfo constructor = null;
                ParameterInfo[] parameterInfoes = null;
                bool isAnonymousType = selectType.IsAnonymousType();

                if (isAnonymousType)
                {
                    constructor = selectType.GetConstructors()[0];
                    parameterInfoes = constructor.GetParameters();
                }

                // Anonymous types have a parameterized constructor
                if (isAnonymousType)
                {

                    var c = ((NewExpression)((LambdaExpression)selectExpression).Body);
                    // Create the parameter array by mapping the joined field names to the constructor parameter names
                    int i = 0;
                    foreach (ParameterInfo parameterInfo in parameterInfoes)
                    {
                        fieldList.Add(Evaluate(c.Arguments[i++]) + (includeAlias ? " AS [" + parameterInfo.Name + "]" : ""));
                    }

                }
                else
                {
                    if (selectType.GetType().IsRuntimeType())
                    {
                        object result = Evaluate(((LambdaExpression)selectExpression).Body);
                        if (result.GetType() == typeof(Dictionary<string, RawSQL>))
                        {

                            foreach (RawSQL field in ((Dictionary<string, RawSQL>)result).Values)
                            {
                                fieldList.Add(field.SQL);
                            }
                        }
                        else
                        {
                            string fullname = ((RawSQL)result).ToString();
                            fieldList.Add("[" + fullname.Substring(fullname.IndexOf(".") + 1) + "]");
                        }
                    }
                    else
                    {
                        foreach (PropertyInfo pi in selectType.GetProperties())
                        {
                            fieldList.Add("[" + pi.Name + "]");
                        }
                    }

                }

            }
            return string.Join(", ", fieldList.ToArray());
        }

        protected object GetReturnVal(object items)
        {
            object returnval = null;

            IList listitems = (IList)items;
            if (hasCount)
                returnval = listitems.Count;
            else if (_hasFirst)
                if (listitems.Count > 0)
                    returnval = listitems[0];
                else
                    returnval = default(T);
            else
                returnval = items;

            return returnval;
        }

    }

}
