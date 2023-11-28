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
            log.LogTrace($"Processing completed {obj.MessageId}");
        }

        private static void OnMessageRollBack(ILogger log, RollBackNotification obj)
        {
            log.LogTrace($"Processing has triggered a rollback {System.Environment.NewLine}{obj.MessageId}{System.Environment.NewLine}{obj.Error}");
        }

        private static void OnPoisonMessage(ILogger log, PoisonMessageNotification obj)
        {
            log.LogTrace($"Processing has triggered a poison message {System.Environment.NewLine}{obj.MessageId}{System.Environment.NewLine}{obj.Error}");
        }

        private static void OnMessageMovedToErrorQueue(ILogger log, ErrorNotification obj)
        {
            log.LogTrace($"Processing has failed {System.Environment.NewLine}{obj.MessageId}{System.Environment.NewLine}{obj.Error}");
        }

        private static void OnReceiveMessageError(ILogger log, ErrorReceiveNotification obj)
        {
            log.LogTrace($"Processing has failed to dequeue a message {System.Environment.NewLine}{obj.Error}");
        }

        private static void OnError(ILogger log, ErrorNotification obj)
        {
            log.LogTrace($"Processing has failed {System.Environment.NewLine}{obj.Error}");
        }
    }
}
