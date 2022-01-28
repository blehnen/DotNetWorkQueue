// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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
using System.Data;
using System.Data.Common;
using DotNetWorkQueue.Transport.Shared;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query
{
    /// <inheritdoc />
    /// <summary>
    /// Dequeue a message from the queue.
    /// </summary>
    public class ReceiveMessageQuery<TConnection, TTransaction> : IQuery<IReceivedMessageInternal>
        where TConnection : IDbConnection
        where TTransaction : IDbTransaction
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReceiveMessageQuery{TConnection, TTransaction}" /> class.
        /// </summary>
        /// <param name="routes">The routes.</param>
        /// <param name="userParameterCollection">user params to use with <see cref="UserWhereClause"/></param>
        /// <param name="userWhereClause">an optional where clause to apply to the de-queue</param>
        public ReceiveMessageQuery(List<string> routes, IReadOnlyList<DbParameter> userParameterCollection, string userWhereClause)
        {
            Connection = default(TConnection);
            Transaction = default(TTransaction);
            Routes = routes;
            UserParameterCollection = userParameterCollection;
            UserWhereClause = userWhereClause;
        }

        /// <summary>Initializes a new instance of the <see cref="ReceiveMessageQuery{TConnection, TTransaction}" /> class.</summary>
        /// <param name="connection">The connection.</param>
        /// <param name="transaction">The transaction.</param>
        /// <param name="routes">The routes.</param>
        /// <param name="userParameterCollection">An optional collection of user params to pass to the query</param>
        /// <param name="userWhereClause">An option user AND clause to pass to the query</param>
        public ReceiveMessageQuery(TConnection connection, TTransaction transaction, List<string> routes, IReadOnlyList<DbParameter> userParameterCollection, string userWhereClause)
        {
            Connection = connection;
            Transaction = transaction;
            Routes = routes;
            UserParameterCollection = userParameterCollection;
            UserWhereClause = userWhereClause;
        }

        /// <summary>
        /// Gets the connection.
        /// </summary>
        /// <value>
        /// The connection.
        /// </value>
        public TConnection Connection { get;  }
        /// <summary>
        /// Gets the transaction.
        /// </summary>
        /// <value>
        /// The transaction.
        /// </value>
        public TTransaction Transaction { get; }
        /// <summary>
        /// Gets the route.
        /// </summary>
        /// <value>
        /// The route.
        /// </value>
        public List<string> Routes { get; }

        /// <summary>
        /// A collection of parameters for <see cref="UserWhereClause"/>
        /// </summary>

        public IReadOnlyList<DbParameter> UserParameterCollection { get; }

        /// <summary>
        /// An additional clause that will be applied to de-queue statements.  See <see cref="UserParameterCollection"/>
        /// </summary>
        public string UserWhereClause { get; }
    }
}
