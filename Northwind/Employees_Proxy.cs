using System.Collections.Generic;
using AccessToLinq;

namespace Northwind
{
    public class Employees_Proxy : Employees, IProxy
    {
        protected Dictionary<string, bool> IsDirty = new Dictionary<string, bool>();

        private string _jobTitle;

        public override string JobTitle
        {
            get => _jobTitle;
            set { _jobTitle = value; SetDirty("JobTitle"); }
        }


        protected void SetDirty(string caller)
        {
            IsDirty[caller] = true;
        }

        public Dictionary<string, bool> GetDirtyProperties()
        {
            return IsDirty;
        }

    }
}