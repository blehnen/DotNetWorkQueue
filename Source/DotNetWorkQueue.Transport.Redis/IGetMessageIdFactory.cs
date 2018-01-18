namespace DotNetWorkQueue.Transport.Redis
{
    /// <summary>
    /// Creates an instance of <see cref="IGetMessageId"/>
    /// </summary>
    public interface IGetMessageIdFactory
    {
        /// <summary>
        /// Creates an instance of <see cref="IGetMessageId"/>
        /// </summary>
        /// <returns></returns>
        IGetMessageId Create();
    }
}
