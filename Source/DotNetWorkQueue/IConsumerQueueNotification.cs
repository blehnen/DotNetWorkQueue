using DotNetWorkQueue.Notifications;
using DotNetWorkQueue.Queue;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Notification for consumer queue message processing
    /// </summary>
    public interface IConsumerQueueNotification
    {
        /// <summary>
        /// The message has been rolled back for re-processing, if possible
        /// </summary>
        /// <param name="rollbackNotification">The rollback information</param>
        void InvokeRollback(RollBackNotification rollbackNotification);

        /// <summary>
        /// The message has completed processing.
        /// </summary>
        /// <param name="messageCompleteNotification">The message that has been completed</param>
        void InvokeMessageComplete(MessageCompleteNotification messageCompleteNotification);

        /// <summary>
        /// Subscribe for user notifications
        /// </summary>
        /// <param name="notifications">User notifications</param>
        void Sub(ConsumerQueueNotifications notifications);
    }
}
