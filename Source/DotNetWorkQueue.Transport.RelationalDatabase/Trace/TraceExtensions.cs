using OpenTracing;
namespace DotNetWorkQueue.Transport.RelationalDatabase.Trace
{
    public static class TraceExtensions
    {
        /// <summary>
        /// Adds the message identifier tag.
        /// </summary>
        /// <param name="span">The span.</param>
        /// <param name="id">The identifier.</param>
        public static void AddMessageIdTag(this ISpan span, long id)
        {
            span.SetTag("MessageId", id.ToString());
        }
    }
}
