using DotNetWorkQueue.Notifications;

namespace DotNetWorkQueue.Queue
{
    internal class ConsumerQueueErrorNotification : IConsumerQueueErrorNotification
    {
        private ConsumerQueueNotifications _notifications;
        public void InvokeError(ErrorNotification error)
        {
            _notifications?.Error?.Invoke(error);
        }

        public void InvokeError(ErrorReceiveNotification error)
        {
            _notifications?.ReceiveMessageError?.Invoke(error);
        }

        public void InvokeMovedToErrorQueue(ErrorNotification error)
        {
            _notifications?.Error?.Invoke(error);
        }

        public void InvokePoisonMessageError(PoisonMessageNotification notification)
        {
            _notifications?.PoisonMessage?.Invoke(notification);
        }

        public void Sub(ConsumerQueueNotifications notifications)
        {
            _notifications = notifications;
        }
    }
}
