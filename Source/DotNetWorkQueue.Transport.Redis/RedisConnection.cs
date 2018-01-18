using System;
using System.Runtime.CompilerServices;
using System.Threading;
using DotNetWorkQueue.Validation;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis
{
    /// <summary>
    /// Contains the connection to the redis server(s)
    /// </summary>
    public class RedisConnection: IRedisConnection
    {
        private readonly IConnectionInformation _connectionInformation;
        private ConnectionMultiplexer _connection;
        private readonly object _connectionLock = new object();
        private int _disposeCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisConnection"/> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        public RedisConnection(IConnectionInformation connectionInformation)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            _connectionInformation = connectionInformation;
        }

        /// <summary>
        /// Gets the connection.
        /// </summary>
        /// <value>
        /// The connection.
        /// </value>
        public ConnectionMultiplexer Connection
        {
            get
            {
                ThrowIfDisposed();
                EnsureCreated();
                return _connection;
            }
        }

        #region IDispose, IIsDisposed
        /// <summary>
        /// Throws an exception if this instance has been disposed.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <exception cref="System.ObjectDisposedException"></exception>
        protected void ThrowIfDisposed([CallerMemberName] string name = "")
        {
            if (Interlocked.CompareExchange(ref _disposeCount, 0, 0) != 0)
            {
                throw new ObjectDisposedException(name);
            }
        }
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            if (Interlocked.Increment(ref _disposeCount) != 1) return;

            _connection?.Dispose();
        }

        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
        public bool IsDisposed => Interlocked.CompareExchange(ref _disposeCount, 0, 0) != 0;

        #endregion

        /// <summary>
        /// Ensures that the connection has been opened.
        /// </summary>
        /// <remarks>The connection will only be opened once</remarks>
        private void EnsureCreated()
        {
            if (_connection != null) return;
            lock (_connectionLock)
            {
                if (_connection != null) return;
                _connection = ConnectionMultiplexer.Connect(_connectionInformation.ConnectionString);
            }
        }
    }
}
