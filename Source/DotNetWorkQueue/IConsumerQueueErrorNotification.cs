using DotNetWorkQueue.Notifications;
using DotNetWorkQueue.Queue;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Interface for notification of consumer queue errors
    /// </summary>
    public interface IConsumerQueueErrorNotification
    {
        /// <summary>
        /// Error while processing a message
        /// </summary>
        /// <param name="error">Error information</param>
        void InvokeError(ErrorNotification error);
        /// <summary>
        /// Error while obtaining messages from transport
        /// </summary>
        /// <param name="error">Error information</param>
        void InvokeError(ErrorReceiveNotification error);
        /// <summary>
        /// A message has been moved to the error queue, if possible.
        /// </summary>
        /// <param name="error">Error information</param>
        void InvokeMovedToErrorQueue(ErrorNotification error);
        /// <summary>
        /// A poison message has been processed
        /// </summary>
        /// <param name="notification">Error information</param>
        void InvokePoisonMessageError(PoisonMessageNotification notification);

        /// <summary>
        /// Subscribe to notifications
        /// </summary>
        /// <param name="notifications">User notifications</param>
        void Sub(ConsumerQueueNotifications notifications);
    }
}
