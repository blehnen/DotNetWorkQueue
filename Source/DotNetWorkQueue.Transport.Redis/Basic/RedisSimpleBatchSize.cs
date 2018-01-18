namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// A very simple class that determines how big a batch should be when sending a batch of messages
    /// </summary>
    internal class RedisSimpleBatchSize : ISendBatchSize
    {
        /// <summary>
        /// Determines the size of the send message batch, based on the total number of messages to be sent
        /// </summary>
        /// <param name="messageCount">The message count.</param>
        /// <returns></returns>
        public int BatchSize(int messageCount)
        {
            if (messageCount <= 50)
            {
                return messageCount;
            }
            if (messageCount < 512)
            {
                return messageCount / 2;
            }
            return 256;
        }
    }
}
