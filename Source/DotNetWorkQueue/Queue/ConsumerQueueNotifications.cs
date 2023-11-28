using DotNetWorkQueue.Notifications;
using System;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Allows subscribing to various consumer queue events.
    /// </summary>
    public class ConsumerQueueNotifications
    {
        /// <summary>
        /// Allows subscribing to various consumer queue events.
        /// </summary>
        /// <param name="onError">Fires when a message has encountered an unhandled exception while processing.</param>
        /// <param name="onReceiveMessageError">Fires when the queue is unable to obtain messages from the transport.</param>
        /// <param name="onMessageMovedToErrorQueue">Fires when the message is moved to the error queue or deleted (if moving to the error queue is not configured)</param>
        /// <param name="onPoisonMessage">Fires when a poison message is encountered.</param>
        /// <param name="onMessageRollBack">Fires when a message is being rolled back.</param>
        /// <param name="onMessageCompleted">Fires when a message has been completed without error.</param>
        public ConsumerQueueNotifications(Action<ErrorNotification> onError = null,
            Action<ErrorReceiveNotification> onReceiveMessageError = null,
            Action<ErrorNotification> onMessageMovedToErrorQueue = null,
            Action<PoisonMessageNotification> onPoisonMessage = null,
            Action<RollBackNotification> onMessageRollBack = null,
            Action<MessageCompleteNotification> onMessageCompleted = null)
        {
            Error = onError;
            MessageCompleted = onMessageCompleted;
            MessageMovedToErrorQueue = onMessageMovedToErrorQueue;
            MessageRollBack = onMessageRollBack;
            PoisonMessage = onPoisonMessage;
            ReceiveMessageError = onReceiveMessageError;
        }

        internal Action<ErrorNotification> Error { get; }

        internal Action<ErrorReceiveNotification> ReceiveMessageError { get; }

        internal Action<ErrorNotification> MessageMovedToErrorQueue { get; }
        internal Action<PoisonMessageNotification> PoisonMessage { get; }
        internal Action<RollBackNotification> MessageRollBack { get; }
        internal Action<MessageCompleteNotification> MessageCompleted { get; }
    }
}
