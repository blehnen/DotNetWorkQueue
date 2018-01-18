namespace DotNetWorkQueue.Transport.Redis
{
    /// <summary>
    /// Gets a new instance of <see cref="IMessageId"/>
    /// </summary>
    public interface IGetMessageId
    {
        /// <summary>
        /// Gets a new instance of <see cref="IMessageId"/>
        /// </summary>
        /// <returns></returns>
        IMessageId Create();
    }
}
