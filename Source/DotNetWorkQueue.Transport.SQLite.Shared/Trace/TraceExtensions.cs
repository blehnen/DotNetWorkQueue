using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;
using OpenTracing;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Trace
{
    public static class TraceExtensions
    {
        public static void Add(this ISpan span, SendMessageCommand command)
        {
            var delay = command.MessageData.GetDelay();
            if (delay.HasValue)
                span.SetTag("MessageDelay",
                    delay.Value.ToString());

            var expiration = command.MessageData.GetExpiration();
            if (expiration.HasValue)
                span.SetTag("MessageExpiration",
                    expiration.Value.ToString());

            var priority = command.MessageData.GetPriority();
            if (priority.HasValue)
                span.SetTag("MessagePriority",
                    priority.Value.ToString());
        }
    }
}
