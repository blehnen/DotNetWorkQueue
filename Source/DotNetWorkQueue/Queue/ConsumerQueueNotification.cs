using DotNetWorkQueue.Notifications;

namespace DotNetWorkQueue.Queue
{
    internal class ConsumerQueueNotification : IConsumerQueueNotification
    {
        private ConsumerQueueNotifications _notifications;

        public void InvokeRollback(RollBackNotification rollbackNotification)
        {
            _notifications?.MessageRollBack?.Invoke(rollbackNotification);
        }

        public void InvokeMessageComplete(MessageCompleteNotification messageCompleteNotification)
        {
            _notifications?.MessageCompleted?.Invoke(messageCompleteNotification);
        }

        public void Sub(ConsumerQueueNotifications notifications)
        {
            _notifications = notifications;
        }
    }
}
