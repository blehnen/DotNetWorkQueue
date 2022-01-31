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
using System.Data;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command
{
    /// <summary>
    /// Sets the status for a status table as part of a transaction
    /// </summary>
    public class SetStatusTableStatusTransactionCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SetStatusTableStatusTransactionCommand" /> class.
        /// </summary>
        /// <param name="queueId">The queue identifier.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="status">The status.</param>
        /// <param name="transaction">The transaction.</param>
        public SetStatusTableStatusTransactionCommand(long queueId,
            IDbConnection connection,
            QueueStatuses status,
            IDbTransaction transaction)
        {
            QueueId = queueId;
            Transaction = transaction;
            Connection = connection;
            Status = status;
        }
        /// <summary>
        /// Gets or sets the queue identifier.
        /// </summary>
        /// <value>
        /// The queue identifier.
        /// </value>
        public long QueueId { get; }

        /// <summary>
        /// Gets the status.
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        public QueueStatuses Status { get; }

        /// <summary>
        /// Gets the connection.
        /// </summary>
        /// <value>
        /// The connection.
        /// </value>
        public IDbConnection Connection { get; }

        /// <summary>
        /// Gets the transaction.
        /// </summary>
        /// <value>
        /// The transaction.
        /// </value>
        public IDbTransaction Transaction { get; }
    }
}
