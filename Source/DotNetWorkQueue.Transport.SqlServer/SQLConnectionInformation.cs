using System.Data.SqlClient;
using DotNetWorkQueue.Configuration;

namespace DotNetWorkQueue.Transport.SqlServer
{
    /// <summary>
    /// Contains connection information for a SQL server queue
    /// </summary>
    public class SqlConnectionInformation: BaseConnectionInformation
    {
        private string _server;
        private string _catalog;

        #region Constructor
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
        /// <summary>
        /// Gets the server.
        /// </summary>
        /// <value>
        /// The server.
        /// </value>
        public override string Server => _server;

        /// <summary>
        /// Gets the container.
        /// </summary>
        /// <value>
        /// The container.
        /// </value>
        /// <remarks>
        /// The name of the container for the queue
        /// </remarks>
        public override string Container => _catalog;

        #endregion

        #region IClone
        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
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
            //validate that the passed in string parses as a SQL server connection string
            var builder = new SqlConnectionStringBuilder(value); //will fail here if not valid
            _server = builder.DataSource;
            _catalog = builder.InitialCatalog;
        }
    }
}
