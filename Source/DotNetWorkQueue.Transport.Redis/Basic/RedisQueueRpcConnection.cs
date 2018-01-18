using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// Connection information for Rpc
    /// </summary>
    public class RedisQueueRpcConnection: BaseRpcConnection
    {
        private readonly string _connection;
        private readonly string _queue;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueueRpcConnection"/> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="queue">The queue.</param>
        public RedisQueueRpcConnection(string connection, string queue)
        {
            Guard.NotNullOrEmpty(() => connection, connection);
            Guard.NotNullOrEmpty(() => queue, queue);
            _connection = connection;
            _queue = queue;
        }

        /// <summary>
        /// Gets the connection.
        /// </summary>
        /// <param name="connectionType">Type of the connection.</param>
        /// <returns></returns>
        public override IConnectionInformation GetConnection(ConnectionTypes connectionType)
        {
            switch (connectionType)
            {
                case ConnectionTypes.NotSpecified:
                case ConnectionTypes.Send:
                    return new BaseConnectionInformation(_queue, _connection);
                case ConnectionTypes.Receive:
                    return new BaseConnectionInformation(string.Concat(_queue, "Response"), _connection);
                default:
                    throw new DotNetWorkQueueException($"unhandled type {connectionType}");
            }
        }
    }
}
