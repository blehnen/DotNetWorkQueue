using System.Threading;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <inheritdoc />
    internal class RedisQueueClearExpiredMessages: IClearExpiredMessages
    {
        /// <inheritdoc />
        public long ClearMessages(CancellationToken cancelToken)
        {
            //the message de-queue handles this; as redis is single threaded, a background thread to remove messages isn't 
            //very helpful, and we need more information when we remove messages anyway
            return 0;
        }
    }
}
