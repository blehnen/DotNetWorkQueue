using DotNetWorkQueue.Transport.Redis.Basic.Command;
using OpenTracing;

namespace DotNetWorkQueue.Transport.Redis.Trace
{
    /// <summary>
    /// Tracing extensions
    /// </summary>
    public static class TraceExtensions
    {
        /// <summary>
        /// Adds tags to the span from the data
        /// </summary>
        /// <param name="span">The span.</param>
        /// <param name="data">The data.</param>
        public static void Add(this ISpan span, IAdditionalMessageData data)
        {
            var delay = data.GetDelay();
            if (delay.HasValue)
                span.SetTag("MessageDelay",
                    delay.Value.ToString());

            var expiration = data.GetExpiration();
            if (expiration.HasValue)
                span.SetTag("MessageExpiration",
                    expiration.Value.ToString());
        }
    }
}
