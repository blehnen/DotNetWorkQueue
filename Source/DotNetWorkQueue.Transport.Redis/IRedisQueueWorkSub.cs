using System;

namespace DotNetWorkQueue.Transport.Redis
{
    /// <summary>
    /// Allows waiting until a channel pushes a notification
    /// </summary>
    public interface IRedisQueueWorkSub: IDisposable, IIsDisposed
    {
        /// <summary>
        /// Waits until a notification is received, or until the timeout is reached
        /// </summary>
        /// <param name="timeout">The timeout. Null value means no timeout.</param>
        /// <returns></returns>
        bool Wait(TimeSpan? timeout);
        /// <summary>
        /// Resets this instance.
        /// </summary>
        void Reset();
    }
}
