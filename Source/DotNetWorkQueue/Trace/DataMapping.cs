using System.Collections;
using System.Collections.Generic;
using OpenTracing.Propagation;

namespace DotNetWorkQueue.Trace
{
    /// <summary>
    /// Storage for open tracing format
    /// </summary>
    public sealed class DataMappingTextMap : Dictionary<string, string>, ITextMap
    {
        /// <inheritdoc />
        public void Set(string key, string value)
        {
            this[key] = value;
        }

        /// <inheritdoc />
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
