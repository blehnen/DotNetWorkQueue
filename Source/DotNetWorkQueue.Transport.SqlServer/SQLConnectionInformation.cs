// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------

using System.Collections.Generic;
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
        public SqlConnectionInformation(QueueConnection queueConnection): base(queueConnection)
        {
            ValidateConnection(queueConnection.Connection);
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
            var data = new Dictionary<string, string>();
            foreach (var keyvalue in AdditionalConnectionSettings)
            {
                data.Add(keyvalue.Key, keyvalue.Value);
            }
            return new SqlConnectionInformation(new QueueConnection(QueueName, ConnectionString, data));
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
