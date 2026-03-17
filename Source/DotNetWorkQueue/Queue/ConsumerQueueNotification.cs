using DotNetWorkQueue.Notifications;

namespace DotNetWorkQueue.Queue
{
    internal class ConsumerQueueNotification : IConsumerQueueNotification
    {
        private readonly IConsumerMetricsNotification _metrics;
        private ConsumerQueueNotifications _notifications;

        public ConsumerQueueNotification(IConsumerMetricsNotification metrics)
        {
            _metrics = metrics;
        }

        public void InvokeRollback(RollBackNotification rollbackNotification)
        {
            _metrics.IncrementRolledBack();
            _notifications?.MessageRollBack?.Invoke(rollbackNotification);
        }

        public void InvokeMessageComplete(MessageCompleteNotification messageCompleteNotification)
        {
            _metrics.IncrementProcessed();
            _notifications?.MessageCompleted?.Invoke(messageCompleteNotification);
        }

        public void Sub(ConsumerQueueNotifications notifications)
        {
            _notifications = notifications;
        }
    }
}
