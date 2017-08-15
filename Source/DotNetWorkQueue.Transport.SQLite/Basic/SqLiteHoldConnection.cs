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
using System;
using System.Data.SQLite;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    internal class SqLiteHoldConnection : System.IDisposable
    {
        private readonly System.Collections.Concurrent.ConcurrentDictionary<string, SQLiteConnection> _connections;

        public SqLiteHoldConnection()
        {
            _connections = new System.Collections.Concurrent.ConcurrentDictionary<string, SQLiteConnection>();
        }

        public void AddConnectionIfNeeded(IConnectionInformation connection)
        {
            var fileName = GetFileNameFromConnectionString.GetFileName(connection.ConnectionString);
            if (!fileName.IsInMemory) return;
            if (_connections.ContainsKey(connection.ConnectionString)) return;
            var sqlConnection = new SQLiteConnection(connection.ConnectionString);
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
                    foreach(var connection in _connections.Values)
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
