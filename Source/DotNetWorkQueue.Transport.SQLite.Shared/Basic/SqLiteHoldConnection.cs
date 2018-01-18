using System;
using System.Collections.Concurrent;
using System.Data;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Basic
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
