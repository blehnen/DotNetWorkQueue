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
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.RelationalDatabase;

namespace DotNetWorkQueue.Transport.SqlServer.Basic
{
    /// <summary>
    /// SqlServer inbox-pattern implementation of <see cref="IRelationalWorkerNotification"/>.
    /// Subclasses <see cref="WorkerNotification"/> and additionally implements
    /// <see cref="IRelationalWorkerNotification"/>, exposing the active dequeue
    /// <see cref="DbTransaction"/> to user message handlers.
    /// </summary>
    /// <remarks>
    /// This class is only registered as the <c>IWorkerNotification</c> binding when
    /// <c>SqlServerMessageQueueTransportOptions.EnableHoldTransactionUntilMessageCommitted</c> is
    /// <see langword="true"/>. With the option off, the container returns a plain
    /// <see cref="WorkerNotification"/> and the capability cast to
    /// <see cref="IRelationalWorkerNotification"/> cleanly fails.
    /// <para>
    /// Property-injection pattern: <see cref="ConnectionHolder"/> is set post-construction by
    /// <c>SqlServerMessageQueueReceive</c> before the user handler is invoked, mirroring the way
    /// <see cref="WorkerNotification.HeartBeat"/> is set today
    /// (<c>HeartBeatWorker.cs</c> line 89). No <see cref="IConnectionHolder{TConnection,TTransaction,TCommand}"/>
    /// is injected via the constructor because the holder is only available on the receive path and
    /// is not container-resolved.
    /// </para>
    /// </remarks>
    internal class SqlServerRelationalWorkerNotification : WorkerNotification, IRelationalWorkerNotification
    {
        /// <summary>
        /// Initializes a new instance of <see cref="SqlServerRelationalWorkerNotification"/>.
        /// All parameters are forwarded unchanged to <see cref="WorkerNotification"/>;
        /// SimpleInjector resolves them from the container with no additional plumbing.
        /// </summary>
        /// <param name="headerNames">The header names.</param>
        /// <param name="cancelWork">The cancel work.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="log">The log.</param>
        /// <param name="metrics">The metrics factory.</param>
        /// <param name="tracer">The tracer.</param>
        public SqlServerRelationalWorkerNotification(
            IHeaders headerNames,
            IQueueCancelWork cancelWork,
            TransportConfigurationReceive configuration,
            ILogger log,
            IMetrics metrics,
            ActivitySource tracer)
            : base(headerNames, cancelWork, configuration, log, metrics, tracer)
        {
        }

        /// <summary>
        /// Gets or sets the active connection holder for the in-flight dequeue operation.
        /// Set post-construction by <c>SqlServerMessageQueueReceive</c> before the user handler
        /// is invoked.
        /// </summary>
        /// <value>
        /// The <see cref="IConnectionHolder{SqlConnection,SqlTransaction,SqlCommand}"/> that wraps
        /// the SqlServer connection and the active dequeue transaction. <see langword="null"/> only
        /// between construction and the receive path's property-injection call; never null during
        /// user-handler execution when the option is enabled.
        /// </value>
        public IConnectionHolder<SqlConnection, SqlTransaction, SqlCommand> ConnectionHolder { get; set; }

        /// <inheritdoc/>
        public DbTransaction Transaction => ConnectionHolder?.Transaction;
    }
}
