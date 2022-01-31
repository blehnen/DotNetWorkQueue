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
using System;
using System.Collections.Concurrent;
using System.Data;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    internal class SqLiteHoldConnection : IDisposable
    {
        private readonly IGetFileNameFromConnectionString _getFileNameFromConnection;
        private readonly IDbFactory _dbFactory;
        private readonly ConcurrentDictionary<string, IDbConnection> _connections;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqLiteHoldConnection"/> class.
        /// </summary>
        /// <param name="getFileNameFromConnection">The get file name from connection.</param>
        /// <param name="dbFactory">The database factory.</param>
        public SqLiteHoldConnection(IGetFileNameFromConnectionString getFileNameFromConnection,
            IDbFactory dbFactory)
        {
            Guard.NotNull(() => getFileNameFromConnection, getFileNameFromConnection);
            Guard.NotNull(() => dbFactory, dbFactory);
            _getFileNameFromConnection = getFileNameFromConnection;
            _dbFactory = dbFactory;
            _connections = new ConcurrentDictionary<string, IDbConnection>();
        }

        public void AddConnectionIfNeeded(IConnectionInformation connection)
        {
            var fileName = _getFileNameFromConnection.GetFileName(connection.ConnectionString);
            if (!fileName.IsInMemory) return;
            if (_connections.ContainsKey(connection.ConnectionString)) return;
            var sqlConnection = _dbFactory.CreateConnection(connection.ConnectionString, true);
            try
            {
                sqlConnection.Open();
            }
            catch (Exception) //resource leak possible on open
            {
                sqlConnection.Dispose();
                throw;
            }
            if (!_connections.TryAdd(connection.ConnectionString, sqlConnection))
            {
                //already added by another thread
                sqlConnection.Dispose();
            }
        }

        #region IDisposable Support
        private bool _disposedValue; // To detect redundant calls
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    foreach (var connection in _connections.Values)
                    {
                        connection.Dispose();
                    }
                    _connections.Clear();
                }
                _disposedValue = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
