// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
using DotNetWorkQueue.Configuration;
using Npgsql;

namespace DotNetWorkQueue.Transport.PostgreSQL
{
    /// <inheritdoc />
    public class SqlConnectionInformation : BaseConnectionInformation
    {
        private string _server;

        #region Constructor
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlConnectionInformation"/> class.
        /// </summary>
        /// <param name="queueConnection">Queue and connection information.</param>
        public SqlConnectionInformation(QueueConnection queueConnection) : base(queueConnection)
        {
            ValidateConnection(queueConnection.Connection);
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
            //validate that the passed in string parses as a connection string
            var builder = new NpgsqlConnectionStringBuilder(value); //will fail here if not valid
            _server = builder.Database;
        }
    }
}
