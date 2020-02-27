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
using System.Data;
using System.Threading;
using Microsoft.Data.Sqlite;

namespace DotNetWorkQueue.Transport.SQLite.Microsoft.Basic
{
    /// <summary>
    /// Enforces that only a single connection can be created per process
    /// </summary>
    /// <seealso cref="System.Data.IDbConnection" />
    public class SingleConnection: IDbConnection
    {
        private readonly SqliteConnection _connection;
        private readonly bool _forMemoryHold;
        private static readonly object Locker = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="SingleConnection"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="forMemoryHold">if set to <c>true</c> [for memory hold].</param>
        public SingleConnection(string connectionString, bool forMemoryHold)
        {
            _forMemoryHold = forMemoryHold;

            if (!_forMemoryHold)
            {
                Monitor.Enter(Locker);
            }
            _connection = new SqliteConnection(connectionString);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _connection.Dispose();
            if (!_forMemoryHold)
            {
                Monitor.Exit(Locker);
            }
        }

        /// <inheritdoc />
        public IDbTransaction BeginTransaction()
        {
            return ((IDbConnection) _connection).BeginTransaction();
        }

        /// <inheritdoc />
        public IDbTransaction BeginTransaction(IsolationLevel il)
        {
            return ((IDbConnection) _connection).BeginTransaction(il);
        }

        /// <inheritdoc />
        public void ChangeDatabase(string databaseName)
        {
            _connection.ChangeDatabase(databaseName);
        }

        /// <inheritdoc />
        public void Close()
        {
            _connection.Close();
        }

        /// <inheritdoc />
        public IDbCommand CreateCommand()
        {
            return ((IDbConnection) _connection).CreateCommand();
        }

        /// <inheritdoc />
        public void Open()
        {
            _connection.Open();
        }

        /// <inheritdoc />
        public string ConnectionString
        {
            get => _connection.ConnectionString;
            set => _connection.ConnectionString = value;
        }

        /// <inheritdoc />
        public int ConnectionTimeout => _connection.ConnectionTimeout;

        /// <inheritdoc />
        public string Database => _connection.Database;

        /// <inheritdoc />
        public ConnectionState State => _connection.State;
    }
}
