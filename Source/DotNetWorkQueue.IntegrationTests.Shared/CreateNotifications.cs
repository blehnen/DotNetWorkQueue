using DotNetWorkQueue.Notifications;
using DotNetWorkQueue.Queue;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.IntegrationTests.Shared
{
    public static class CreateNotifications
    {
        public static ConsumerQueueNotifications Create(ILogger logger)
        {
            var notifications =
                new ConsumerQueueNotifications((notification) => OnError(logger, notification),
                    (notification) => OnReceiveMessageError(logger, notification),
                    (notification) => OnMessageMovedToErrorQueue(logger, notification),
                    (notification) => OnPoisonMessage(logger, notification),
                    (notification) => OnMessageRollBack(logger, notification),
                    (notification) => OnMessageCompleted(logger, notification));
            return notifications;
        }
        private static void OnMessageCompleted(ILogger log, MessageCompleteNotification obj)
        {
            if (log.IsEnabled(LogLevel.Trace))
                log.LogTrace("Processing completed {MessageId}", obj.MessageId);
        }

        private static void OnMessageRollBack(ILogger log, RollBackNotification obj)
        {
            if (log.IsEnabled(LogLevel.Trace))
                log.LogTrace(obj.Error, "Processing has triggered a rollback {MessageId}", obj.MessageId);
        }

        private static void OnPoisonMessage(ILogger log, PoisonMessageNotification obj)
        {
            if (log.IsEnabled(LogLevel.Trace))
                log.LogTrace(obj.Error, "Processing has triggered a poison message {MessageId}", obj.MessageId);
        }

        private static void OnMessageMovedToErrorQueue(ILogger log, ErrorNotification obj)
        {
            if (log.IsEnabled(LogLevel.Trace))
                log.LogTrace(obj.Error, "Processing has failed {MessageId}", obj.MessageId);
        }

        private static void OnReceiveMessageError(ILogger log, ErrorReceiveNotification obj)
        {
            if (log.IsEnabled(LogLevel.Trace))
                log.LogTrace(obj.Error, "Processing has failed to dequeue a message");
        }

        private static void OnError(ILogger log, ErrorNotification obj)
        {
            if (log.IsEnabled(LogLevel.Trace))
                log.LogTrace(obj.Error, "Processing has failed");
        }
    }
}
