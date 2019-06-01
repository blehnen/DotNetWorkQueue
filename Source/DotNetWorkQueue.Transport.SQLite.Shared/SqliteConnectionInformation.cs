using DotNetWorkQueue.Configuration;

namespace DotNetWorkQueue.Transport.SQLite.Shared
{
    /// <inheritdoc />
    public class SqliteConnectionInformation: BaseConnectionInformation
    {
        private readonly IDbDataSource _dataSource;
        private string _server;

        #region Constructor
        /// <inheritdoc />
        public SqliteConnectionInformation(string queueName, string connectionString, IDbDataSource dataSource) : base(queueName, connectionString)
        {
            _dataSource = dataSource;
            ValidateConnection(connectionString);
        }
        #endregion

        #region Public Properties

        /// <inheritdoc />
        public override string Server => _server;

        /// <inheritdoc />
        public override string Container => Server;
        #endregion

        #region IClone
        /// <inheritdoc />
        public override IConnectionInformation Clone()
        {
            return new SqliteConnectionInformation(QueueName, ConnectionString, _dataSource);
        }
        #endregion

        /// <summary>
        /// Validates the connection string and determines the value of the server property
        /// </summary>
        /// <param name="value">The value.</param>
        /// <remarks>Connection strings that are in an invalid format will cause an exception</remarks>
        private void ValidateConnection(string value)
        {
            //validate that the passed in string parses as a SQLite server connection string
            if (_dataSource != null)
            {
                _server = _dataSource.DataSource(value);
            }
        }
    }
}
