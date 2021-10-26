using System.Collections.Generic;
using System.Linq;

namespace AccessToLinq
{
    /// <summary>
    /// Reference only
    /// </summary>
    public class ProxyBase
    {
        protected bool IsTrackingEnabled;
        protected Dictionary<string, bool> IsDirty = new Dictionary<string, bool>();

        protected void SetDirty(string caller)
        {
            if (IsTrackingEnabled)
            {
                IsDirty[caller] = true;
            }
        }

        public IEnumerable<string> GetDirtyProperties()
        {
            return IsDirty.Where(p => p.Value).Select(p => p.Key);
        }

    }
}