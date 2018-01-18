using System.Linq;
using DotNetWorkQueue.Configuration;
using StackExchange.Redis;

namespace DotNetWorkQueue.Transport.Redis
{
    internal class RedisConnectionInfo: BaseConnectionInformation
    {
        private string _server;

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisConnectionInfo"/> class.
        /// </summary>
        /// <param name="queueName">Name of the queue.</param>
        /// <param name="connectionString">The connection string.</param>
        public RedisConnectionInfo(string queueName, string connectionString): base(queueName, connectionString)
        {
            ValidateConnection(connectionString);
        }
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
            return new RedisConnectionInfo(QueueName, ConnectionString);
        }
        #endregion

        /// <summary>
        /// Gets the server.
        /// </summary>
        /// <value>
        /// The server.
        /// </value>
        public override string Server => _server;

        /// <summary>
        /// Validates the connection string and determines the value of the server property
        /// </summary>
        /// <param name="value">The value.</param>
        /// <remarks>Connection strings that are in an invalid format will cause an exception</remarks>
        private void ValidateConnection(string value)
        {
            //validate that the passed in string parses as a redis connection string
            var options = ConfigurationOptions.Parse(value);
            _server = string.Join(",", options.EndPoints.Select(endpoint => endpoint.ToString()).ToList());
        }
    }
}
