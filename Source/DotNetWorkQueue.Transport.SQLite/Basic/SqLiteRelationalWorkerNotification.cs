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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.RelationalDatabase;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// SQLite inbox-pattern implementation of <see cref="IRelationalWorkerNotification"/>.
    /// Subclasses <see cref="WorkerNotification"/> and additionally implements
    /// <see cref="IRelationalWorkerNotification"/>, exposing the active dequeue
    /// <see cref="DbTransaction"/> to user message handlers via Phase 5's
    /// hold-transaction infrastructure.
    /// </summary>
    /// <remarks>
    /// This class is only registered as the <c>IWorkerNotification</c> binding when
    /// <c>SqLiteMessageQueueTransportOptions.EnableHoldTransactionUntilMessageCommitted</c>
    /// is <see langword="true"/>. With the option off, the container returns a plain
    /// <see cref="WorkerNotification"/> and the capability cast to
    /// <see cref="IRelationalWorkerNotification"/> cleanly fails.
    /// <para>
    /// Property-injection pattern (mirrors SqlServer/PostgreSQL counterparts):
    /// <see cref="ConnectionState"/> is set post-construction by
    /// <c>SqLiteMessageQueueReceive.ContextOnDequeueComplete</c> (or equivalent) before
    /// the user handler is invoked. The receive path pattern-matches the resolved
    /// <see cref="IWorkerNotification"/> as this type and sets the state from the
    /// <see cref="SqLiteConnectionState"/> it stored on <see cref="IMessageContext"/>.
    /// </para>
    /// </remarks>
    internal class SqLiteRelationalWorkerNotification : WorkerNotification, IRelationalWorkerNotification
    {
        /// <summary>
        /// Initializes a new instance of <see cref="SqLiteRelationalWorkerNotification"/>.
        /// All parameters are forwarded unchanged to <see cref="WorkerNotification"/>;
        /// SimpleInjector resolves them from the container with no additional plumbing.
        /// </summary>
        /// <param name="headerNames">The header names.</param>
        /// <param name="cancelWork">The cancel work.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="log">The log.</param>
        /// <param name="metrics">The metrics factory.</param>
        /// <param name="tracer">The tracer.</param>
        public SqLiteRelationalWorkerNotification(
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
        /// Gets or sets the active per-message connection-and-transaction state. Set
        /// post-construction by <c>SqLiteMessageQueueReceive</c> (via pattern-match on
        /// the resolved <see cref="IWorkerNotification"/>) before the user handler runs.
        /// Null only between construction and the receive-path injection call.
        /// </summary>
        public SqLiteConnectionState ConnectionState { get; set; }

        /// <inheritdoc/>
        /// <remarks>
        /// Returns the held <see cref="System.Data.IDbTransaction"/> upcast to
        /// <see cref="DbTransaction"/>. At runtime, the underlying instance is a
        /// <c>Microsoft.Data.Sqlite.SqliteTransaction</c> (or compatible) which derives
        /// from <see cref="DbTransaction"/>, so the cast succeeds. Returns null only if
        /// <see cref="ConnectionState"/> is unset (between construction and the receive
        /// path's injection call) — never during user-handler execution when this class
        /// is in scope.
        /// </remarks>
        public DbTransaction Transaction => ConnectionState?.Transaction as DbTransaction;
    }
}
