using System;
using System.Data;
using System.Data.Odbc;
using System.Reflection;

namespace AccessToLinq
{
    public abstract class AccessContext : IDisposable
    {
        protected string ConnectionString;
        protected IDbConnection Connection;
        
        private void Init()
        {
            //var fields = this.GetType().GetFields();
            //foreach (var field in fields)
            //{
            //    if (field.FieldType.IsGenericType && (field.FieldType.GetGenericTypeDefinition() == typeof(AccessDBSet<>)))
            //    {
            //        var types = field.FieldType.GetGenericArguments();
            //        field.SetValue(this, Activator.CreateInstance(typeof(AccessDBSet<>).MakeGenericType(types[0]), new object[] { this }));
            //    }
            //}

            var properties = this.GetType().GetProperties();
            foreach (var property in properties)
            {
                if (property.PropertyType.IsGenericType && (property.PropertyType.GetGenericTypeDefinition() == typeof(AccessDBSet<>)))
                {
                    var types = property.PropertyType.GetGenericArguments();
                    property.SetValue(this, Activator.CreateInstance(typeof(AccessDBSet<>).MakeGenericType(types[0]), new object[] { this }));
                }
            }
        }


        public virtual void OnModelCreating()
        {

        }

        protected AccessContext(IDbConnection connection)
        {
            this.Connection = connection;
            Init();
        }

        protected AccessContext(string connectionString)
        {
            this.ConnectionString = connectionString;
            Init();
        }

        public void AddObject(string entitySetName, object entity)
        {
            var fields = this.GetType().GetProperties();
            foreach (PropertyInfo field in fields)
            {
                if (field.PropertyType.IsGenericType)
                {
                    Type[] types = field.PropertyType.GetGenericArguments();
                    if (types[0].Name == entitySetName)
                    {
                        dynamic set = field.GetValue(this, null);
                        set.AddObject(entity);
                    }
                }
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
            }
        }

        #endregion

        public virtual IDbConnection CreateConnection()
        {
            return new OdbcConnection(ConnectionString);
        }

        internal object Execute(string commandText)
        {
            object retval = null;
            using (var connection = CreateConnection())
            {
                connection.Open();
                var cmd = connection.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = commandText;
                cmd.ExecuteNonQuery();
                if (commandText.ToUpper().StartsWith("INSERT"))
                {
                    cmd.CommandText = "Select @@Identity";
                    retval = cmd.ExecuteScalar();
                }
                connection.Close();
                return retval;
            }
        }

        public void SaveChanges()
        {
        }
    }
}
