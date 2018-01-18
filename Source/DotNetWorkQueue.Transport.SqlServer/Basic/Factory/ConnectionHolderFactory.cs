using System;
using System.Data.SqlClient;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SqlServer.Basic.Factory
{
    /// <summary>
    /// 
    /// </summary>
    public class ConnectionHolderFactory : IConnectionHolderFactory<SqlConnection, SqlTransaction, SqlCommand>
    {
        private readonly IConnectionInformation _connectionInfo;
        private readonly Lazy<SqlServerMessageQueueTransportOptions> _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionHolderFactory" /> class.
        /// </summary>
        /// <param name="connectionInfo">The connection information.</param>
        /// <param name="options">The options.</param>
        public ConnectionHolderFactory(IConnectionInformation connectionInfo,
            ISqlServerMessageQueueTransportOptionsFactory options)
        {
            Guard.NotNull(() => connectionInfo, connectionInfo);
            Guard.NotNull(() => options, options);

            _connectionInfo = connectionInfo;
            _options = new Lazy<SqlServerMessageQueueTransportOptions>(options.Create);
        }
        /// <summary>
        /// Creates a new instance of <see cref="T:DotNetWorkQueue.Transport.RelationalDatabase.IConnectionHolder`3" />
        /// </summary>
        /// <returns></returns>
        public IConnectionHolder<SqlConnection, SqlTransaction, SqlCommand> Create()
        {
            return new ConnectionHolder(_connectionInfo, _options.Value);
        }
    }
}
