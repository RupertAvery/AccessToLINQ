using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Collections;

namespace AccessToLinq
{

    public abstract class QueryProvider : IQueryProvider
    {
        protected QueryProvider()
        {
        }

        #region IQueryProvider Members

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new Query<TElement>(this, expression);
        }

        public IQueryable CreateQuery(Expression expression)
        {
            Type elementType = TypeSystem.GetElementType(expression.Type);
            try
            {
                return (IQueryable)Activator.CreateInstance(typeof(Query<>).MakeGenericType(elementType), new object[] { this, expression });
            }
            catch (TargetInvocationException tie)
            {
                throw tie.InnerException;
            }
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return (TResult)this.ExecuteExpression(expression);
        }

        public object Execute(Expression expression)
        {
            return this.ExecuteExpression(expression);
        }

        #endregion

        protected abstract object ExecuteExpression(Expression expression);


    }

   

}
