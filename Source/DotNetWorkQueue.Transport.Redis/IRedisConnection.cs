using System;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis
{
    /// <summary>
    /// Defines a connection to a redis server(s)
    /// </summary>
    public interface IRedisConnection: IDisposable, IIsDisposed
    {
        /// <summary>
        /// Gets the connection.
        /// </summary>
        /// <value>
        /// The connection.
        /// </value>
        ConnectionMultiplexer Connection { get; }
    }
}
