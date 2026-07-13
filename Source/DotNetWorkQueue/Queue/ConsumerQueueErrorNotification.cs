using System.Diagnostics.CodeAnalysis;
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

        [SuppressMessage("Major Code Smell", "S4144:Methods should not have identical implementations", Justification = "distinct domain events (error raised vs. moved to error queue); kept separate so they can diverge without touching callers")]
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
