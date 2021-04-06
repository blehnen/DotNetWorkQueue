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
using System.Data.SQLite;
using DotNetWorkQueue.Transport.SQLite;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// Creates new db objects
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.Transport.SQLite.IDbFactory" />
    public class DbFactory: IDbFactory
    {
        private readonly IContainer _container;
        /// <summary>
        /// Initializes a new instance of the <see cref="DbFactory"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        public DbFactory(IContainerFactory container)
        {
            _container = container.Create();
        }

        /// <inheritdoc />
        public IDbConnection CreateConnection(string connectionString, bool forMemoryHold)
        {
            return new SQLiteConnection(connectionString);
        }

        /// <inheritdoc />
        public IDbCommand CreateCommand(IDbConnection connection)
        {
            return connection.CreateCommand();
        }

        /// <inheritdoc />
        public ISQLiteTransactionWrapper CreateTransaction(IDbConnection connection)
        {
            var transaction = _container.GetInstance<ISQLiteTransactionWrapper>();
            transaction.Connection = connection;
            return transaction;
        }
    }
}
