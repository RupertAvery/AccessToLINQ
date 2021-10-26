using System.Linq.Expressions;

namespace AccessToLINQ.Expressions
{
    class Lambda
    {
        public LambdaExpression callexp;
        public object p;

        public Lambda(LambdaExpression callexp, object p)
        {
            // TODO: Complete member initialization
            this.callexp = callexp;
            this.p = p;
        }

    }
}