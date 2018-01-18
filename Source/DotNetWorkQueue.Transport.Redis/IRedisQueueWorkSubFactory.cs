namespace DotNetWorkQueue.Transport.Redis
{
    /// <summary>
    /// Creates new instance of <see cref="IRedisQueueWorkSub"/>
    /// </summary>
    internal interface IRedisQueueWorkSubFactory
    {
        /// <summary>
        /// Creates new instance of <see cref="IRedisQueueWorkSub"/> that will only respond if the specified ID is sent
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        IRedisQueueWorkSub Create(IMessageId id);
        /// <summary>
        /// Creates new instance of <see cref="IRedisQueueWorkSub"/>
        /// </summary>
        /// <returns></returns>
        IRedisQueueWorkSub Create();
    }
}
