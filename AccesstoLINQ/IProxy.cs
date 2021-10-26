using System.Collections.Generic;

namespace AccessToLinq
{
    public interface IProxy
    {
        Dictionary<string, bool> GetDirtyProperties();
    }
}