namespace DotNetWorkQueue.Transport.Redis.Basic.MessageID
{
    /// <inheritdoc />
    /// <summary>
    /// Returns a new ID will no value; this will tell the LUA script to generate an ID
    /// </summary>
    internal class GetRedisIncrId: IGetMessageId
    {
        /// <inheritdoc />
        public IMessageId Create()
        {
            return new RedisQueueId(string.Empty);
        }
    }
}
