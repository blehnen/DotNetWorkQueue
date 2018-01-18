using System;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Validation;
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic.Factory
{
    /// <inheritdoc />
    public class ConnectionHolderFactory : IConnectionHolderFactory<NpgsqlConnection, NpgsqlTransaction, NpgsqlCommand>
    {
        private readonly IConnectionInformation _connectionInfo;
        private readonly Lazy<PostgreSqlMessageQueueTransportOptions> _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionHolderFactory" /> class.
        /// </summary>
        /// <param name="connectionInfo">The connection information.</param>
        /// <param name="options">The options.</param>
        public ConnectionHolderFactory(IConnectionInformation connectionInfo,
            IPostgreSqlMessageQueueTransportOptionsFactory options)
        {
            Guard.NotNull(() => connectionInfo, connectionInfo);
            Guard.NotNull(() => options, options);

            _connectionInfo = connectionInfo;
            _options = new Lazy<PostgreSqlMessageQueueTransportOptions>(options.Create);
        }
        /// <inheritdoc />
        public IConnectionHolder<NpgsqlConnection, NpgsqlTransaction, NpgsqlCommand> Create()
        {
            return new ConnectionHolder(_connectionInfo, _options.Value);
        }
    }
}
