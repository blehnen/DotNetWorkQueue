using System;

namespace DotNetWorkQueue.Transport.Redis.Basic.Factory
{
    /// <inheritdoc />
    internal class RedisQueueCorrelationIdFactory: ICorrelationIdFactory
    {
        /// <inheritdoc />
        public ICorrelationId Create()
        {
            return new RedisQueueCorrelationId(Guid.NewGuid());
        }
    }
}
