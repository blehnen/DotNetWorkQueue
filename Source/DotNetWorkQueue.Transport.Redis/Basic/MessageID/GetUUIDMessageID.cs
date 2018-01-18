using System;

namespace DotNetWorkQueue.Transport.Redis.Basic.MessageID
{
    /// <inheritdoc />
    internal class GetUuidMessageId: IGetMessageId
    {
        /// <inheritdoc />
        public IMessageId Create()
        {
            return new RedisQueueId(Guid.NewGuid().ToString());
        }
    }
}
