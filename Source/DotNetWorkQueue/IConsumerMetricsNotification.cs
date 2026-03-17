namespace DotNetWorkQueue
{
    /// <summary>
    /// Receives notifications when consumer message processing events occur.
    /// Implement this interface to track consumer metrics (e.g. in a dashboard).
    /// </summary>
    public interface IConsumerMetricsNotification
    {
        /// <summary>Called when a message has been successfully processed.</summary>
        void IncrementProcessed();

        /// <summary>Called when a message processing error occurs.</summary>
        void IncrementErrored();

        /// <summary>Called when a message is rolled back for re-processing.</summary>
        void IncrementRolledBack();

        /// <summary>Called when a poison message is detected.</summary>
        void IncrementPoisonMessage();
    }
}
