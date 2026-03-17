using DotNetWorkQueue.Notifications;

namespace DotNetWorkQueue.Queue
{
    internal class ConsumerQueueErrorNotification : IConsumerQueueErrorNotification
    {
        private readonly IConsumerMetricsNotification _metrics;
        private ConsumerQueueNotifications _notifications;

        public ConsumerQueueErrorNotification(IConsumerMetricsNotification metrics)
        {
            _metrics = metrics;
        }

        public void InvokeError(ErrorNotification error)
        {
            _metrics.IncrementErrored();
            _notifications?.Error?.Invoke(error);
        }

        public void InvokeError(ErrorReceiveNotification error)
        {
            _notifications?.ReceiveMessageError?.Invoke(error);
        }

        public void InvokeMovedToErrorQueue(ErrorNotification error)
        {
            _metrics.IncrementErrored();
            _notifications?.Error?.Invoke(error);
        }

        public void InvokePoisonMessageError(PoisonMessageNotification notification)
        {
            _metrics.IncrementPoisonMessage();
            _notifications?.PoisonMessage?.Invoke(notification);
        }

        public void Sub(ConsumerQueueNotifications notifications)
        {
            _notifications = notifications;
        }
    }
}
