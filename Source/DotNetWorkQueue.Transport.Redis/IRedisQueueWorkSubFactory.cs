namespace DotNetWorkQueue.Transport.Redis
{
    /// <summary>
    /// Creates new instance of <see cref="IRedisQueueWorkSub"/>
    /// </summary>
    internal interface IRedisQueueWorkSubFactory
    {
        /// <summary>
        /// Creates new instance of <see cref="IRedisQueueWorkSub"/>
        /// </summary>
        /// <returns></returns>
        IRedisQueueWorkSub Create();
    }
}
