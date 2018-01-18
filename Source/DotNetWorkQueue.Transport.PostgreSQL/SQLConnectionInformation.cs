using DotNetWorkQueue.Configuration;
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL
{
    /// <inheritdoc />
    public class SqlConnectionInformation: BaseConnectionInformation
    {
        private string _server;

        #region Constructor
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlConnectionInformation"/> class.
        /// </summary>
        /// <param name="queueName">Name of the queue.</param>
        /// <param name="connectionString">The connection string.</param>
        public SqlConnectionInformation(string queueName, string connectionString): base(queueName, connectionString)
        {
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
            return new SqlConnectionInformation(QueueName, ConnectionString);
        }
        #endregion

        /// <summary>
        /// Validates the connection string and determines the value of the server property
        /// </summary>
        /// <param name="value">The value.</param>
        /// <remarks>Connection strings that are in an invalid format will cause an exception</remarks>
        private void ValidateConnection(string value)
        {
            //validate that the passed in string parses as a connection string
            var builder = new NpgsqlConnectionStringBuilder(value); //will fail here if not valid
            _server = builder.Database;
        }
    }
}
