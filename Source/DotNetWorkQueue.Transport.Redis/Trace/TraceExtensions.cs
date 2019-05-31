using DotNetWorkQueue.Transport.Redis.Basic.Command;
using OpenTracing;

namespace DotNetWorkQueue.Transport.Redis.Trace
{
    public static class TraceExtensions
    {
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
