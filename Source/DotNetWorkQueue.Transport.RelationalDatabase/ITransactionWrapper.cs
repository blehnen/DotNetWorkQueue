// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using System.Data.Common;
using System.Threading.Tasks;

namespace DotNetWorkQueue.Transport.RelationalDatabase
{
    /// <summary>
    /// Wraps a transaction to allow for custom interception
    /// </summary>
    /// <remarks>Since we can't really use IoC to directly create our transactions</remarks>
    public interface ITransactionWrapper
    {
        /// <summary>
        /// Gets or sets the connection.
        /// </summary>
        /// <value>
        /// The connection.
        /// </value>
        DbConnection Connection { get; set; }
        /// <summary>
        /// Begins the transaction.
        /// </summary>
        /// <returns></returns>
        DbTransaction BeginTransaction();
        /// <summary>
        /// Begins the transaction without blocking a thread across the call.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// On SQL Server and PostgreSQL beginning a transaction is a round trip, so the asynchronous
        /// consumer needs this rather than the synchronous member; the send path already awaits
        /// <see cref="DbConnection.BeginTransactionAsync(System.Threading.CancellationToken)"/> directly.
        /// A provider that does not override it runs the synchronous version on the calling thread,
        /// which is what SQLite does.
        /// </remarks>
        Task<DbTransaction> BeginTransactionAsync();
    }
}
