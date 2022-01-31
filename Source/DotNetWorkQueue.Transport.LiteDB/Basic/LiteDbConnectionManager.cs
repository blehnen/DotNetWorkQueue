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
using DotNetWorkQueue.Validation;
using LiteDB;

namespace DotNetWorkQueue.Transport.LiteDb.Basic
{
    /// <summary>
    /// Returns a connection for LiteDb
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class LiteDbConnectionManager : IDisposable
    {
        private readonly IConnectionInformation _connectionInformation;

        private readonly object _createLocker = new object();
        private volatile LiteDatabase _db;

        private readonly bool _shared;
        private readonly bool _shouldDisposeDirectConnection;

        /// <summary>
        /// Initializes a new instance of the <see cref="LiteDbConnectionManager"/> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="scope">The scope.</param>
        public LiteDbConnectionManager(IConnectionInformation connectionInformation, ICreationScope scope)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => scope, scope);
            _connectionInformation = connectionInformation;

            var builder = new LiteDB.ConnectionString(_connectionInformation.ConnectionString);
            _shared = builder.Connection == ConnectionType.Shared;

            var existingScopeConnection = scope.GetDisposable<LiteDbConnectionManager>();
            if (existingScopeConnection != null)
            {
                _db = existingScopeConnection.GetDatabase().Database;
                _shouldDisposeDirectConnection = false;
            }
            else
            {
                _shouldDisposeDirectConnection = true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is shared connection.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is shared connection; otherwise, <c>false</c>.
        /// </value>
        public bool IsSharedConnection => _shared;

        /// <summary>
        /// Gets the database.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException">_db</exception>
        public LiteDbConnection GetDatabase()
        {
            if (_shared || _disposedValue) //if shared or if the manager has been disposed, return an instance that is disposable
                return new LiteDbConnection(new LiteDatabase(_connectionInformation.ConnectionString), true);

            if (_db != null)
                return new LiteDbConnection(_db, false);

            lock (_createLocker)
            {
                if (_db == null && !_disposedValue)
                {
                    _db = new LiteDatabase(_connectionInformation.ConnectionString);
                }
            }

            //recheck
            if (_disposedValue)
                throw new ObjectDisposedException("_db");

            return new LiteDbConnection(_db, false);
        }

        #region IDisposable Support
        private bool _disposedValue; // To detect redundant calls
        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    lock (_createLocker)
                    {
                        if (_shouldDisposeDirectConnection || _shared)
                        {
                            _db?.Dispose();
                            _db = null;
                            _disposedValue = true;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }
        #endregion
    }
}
