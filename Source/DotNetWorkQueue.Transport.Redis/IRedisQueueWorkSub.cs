using System;

namespace DotNetWorkQueue.Transport.Redis
{
    /// <summary>
    /// Allows waiting until a channel pushes a notification
    /// </summary>
    public interface IRedisQueueWorkSub: IDisposable, IIsDisposed
    {
        /// <summary>
        /// Waits until a notification is received
        /// </summary>
        /// <returns></returns>
        bool Wait();
        /// <summary>
        /// Resets this instance.
        /// </summary>
        void Reset();
    }
}
