// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
            if (_dataSource != null) //right now, this is null for RPC
            {
                _server = _dataSource.DataSource(value);
            }
        }
    }
}
