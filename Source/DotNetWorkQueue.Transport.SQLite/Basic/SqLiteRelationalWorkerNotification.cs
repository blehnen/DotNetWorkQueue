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
using System.Threading;
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
    /// State storage uses an <see cref="AsyncLocal{T}"/> instead of an instance property
    /// so the lookup is robust against the <c>IWorkerNotification</c> being registered
    /// transient: even if the SimpleInjector resolution returns a different
    /// <see cref="SqLiteRelationalWorkerNotification"/> instance to the receive-path
    /// pattern-match versus the handler, every instance observes the SAME per-async-flow
    /// state. SqlServer/PostgreSQL counterparts avoid this issue because their typed
    /// <c>IConnectionHolder</c> abstraction is set on the context (singleton header key)
    /// and the notification reads it back via the holder reference; SQLite uses
    /// <see cref="AsyncLocal{T}"/> here because the SQLite receive pipeline operates
    /// on the un-typed <see cref="System.Data.IDbConnection"/> / <see cref="System.Data.IDbTransaction"/>
    /// interfaces throughout, so introducing a parallel typed holder would have been an
    /// unnecessary abstraction.
    /// </para>
    /// <para>
    /// Lifecycle: the receive path sets state via <see cref="SetCurrent"/> after the
    /// dequeue transaction is created, and clears it via <see cref="ClearCurrent"/> in
    /// the context-cleanup delegate. <see cref="ClearCurrent"/> MUST run on the same
    /// async flow before the worker picks up the next message — otherwise the next
    /// handler would observe stale state. Both are wired up by
    /// <c>SqLiteMessageQueueReceive.SetActionsOnContext</c> and the inner
    /// <c>ReceiveMessage.GetMessage</c>.
    /// </para>
    /// </remarks>
    internal class SqLiteRelationalWorkerNotification : WorkerNotification, IRelationalWorkerNotification
    {
        private static readonly AsyncLocal<SqLiteConnectionState> CurrentState = new AsyncLocal<SqLiteConnectionState>();

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
        /// Sets the active per-message connection-and-transaction state on the
        /// current async flow. Called by the receive path after the dequeue
        /// connection + transaction are created. Read by <see cref="Transaction"/>
        /// during user-handler execution on the same async flow.
        /// </summary>
        /// <param name="state">The state to install for the current async flow.</param>
        public static void SetCurrent(SqLiteConnectionState state) => CurrentState.Value = state;

        /// <summary>
        /// Clears the active per-message connection-and-transaction state on the
        /// current async flow. MUST be called in the context-cleanup delegate so
        /// the next message handled by the same worker thread does not observe
        /// stale state.
        /// </summary>
        public static void ClearCurrent() => CurrentState.Value = null;

        /// <summary>
        /// Gets or sets the active per-message connection-and-transaction state.
        /// The setter forwards to <see cref="SetCurrent"/> so the existing
        /// receive-path pattern-match injection continues to work; getter reads
        /// the same async-local slot. Preserved for the unit-test contract.
        /// </summary>
        public SqLiteConnectionState ConnectionState
        {
            get => CurrentState.Value;
            set => CurrentState.Value = value;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Returns the held <see cref="System.Data.IDbTransaction"/> upcast to
        /// <see cref="DbTransaction"/>. At runtime, the underlying instance is a
        /// <c>System.Data.SQLite.SQLiteTransaction</c> which derives from
        /// <see cref="DbTransaction"/>, so the cast succeeds. Returns null only
        /// outside an active dequeue (i.e., between worker idle and the receive
        /// path installing state) — never during user-handler execution when
        /// hold-transaction mode is enabled.
        /// </remarks>
        public DbTransaction Transaction => CurrentState.Value?.Transaction as DbTransaction;
    }
}
