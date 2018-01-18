namespace DotNetWorkQueue.Transport.Redis
{
    /// <summary>
    /// Determines the size of the send message batch, based on the total number of messages to be sent
    /// </summary>
    public interface ISendBatchSize
    {
        /// <summary>
        /// Determines the size of the send message batch, based on the total number of messages to be sent
        /// </summary>
        /// <param name="messageCount">The message count.</param>
        /// <returns></returns>
        int BatchSize(int messageCount);
    }
}
