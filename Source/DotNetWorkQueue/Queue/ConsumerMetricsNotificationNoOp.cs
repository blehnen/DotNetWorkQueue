namespace DotNetWorkQueue.Queue
{
    internal class ConsumerMetricsNotificationNoOp : IConsumerMetricsNotification
    {
        public void IncrementProcessed() { }
        public void IncrementErrored() { }
        public void IncrementRolledBack() { }
        public void IncrementPoisonMessage() { }
    }
}
