using DotNetWorkQueue.Exceptions;

namespace DotNetWorkQueue.Notifications
{
    /// <summary>
    /// A poison message notification.
    /// </summary>
    public class PoisonMessageNotification : ABaseNotification
    {
        /// <summary>
        /// A poison message notification.
        /// </summary>
        /// <param name="error">The error.</param>
        public PoisonMessageNotification(PoisonMessageException error) : base(error.MessageId, error.CorrelationId, error.Headers)
        {
            Error = error;
        }

        /// <summary>
        /// The exception that occurred.
        /// </summary>
        public PoisonMessageException Error { get; }
    }
}
