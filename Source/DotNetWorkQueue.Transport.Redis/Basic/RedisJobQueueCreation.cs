using System;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <inheritdoc />
    public class RedisJobQueueCreation : IJobQueueCreation
    {
        private readonly RedisQueueCreation _creation;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisJobQueueCreation"/> class.
        /// </summary>
        /// <param name="creation">The creation.</param>
        public RedisJobQueueCreation(RedisQueueCreation creation)
        {
            _creation = creation;
        }
        /// <inheritdoc />
        public bool IsDisposed => _creation.IsDisposed;

        /// <inheritdoc />
        public ICreationScope Scope => _creation.Scope;

        /// <inheritdoc />
        public QueueCreationResult CreateJobSchedulerQueue(Action<IContainer> registerService, string queue, string connection, Action<IContainer> setOptions = null, bool enableRoute = false)
        {
            return _creation.CreateQueue();
        }

        /// <inheritdoc />
        public QueueRemoveResult RemoveQueue()
        {
            return _creation.RemoveQueue();
        }

        #region IDisposable Support
        private bool _disposedValue; // To detect redundant calls

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    _creation.Dispose();
                }
                _disposedValue = true;
            }
        }
        /// <inheritdoc />
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion

    }
}
