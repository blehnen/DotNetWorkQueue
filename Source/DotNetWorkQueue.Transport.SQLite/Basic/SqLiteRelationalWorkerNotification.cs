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
    /// SQLite differs from SqlServer/PostgreSQL: instead of injecting a typed
    /// <see cref="IConnectionHolder{TConnection,TTransaction,TCommand}"/> via property
    /// injection, the SQLite implementation reads the active <see cref="SqLiteConnectionState"/>
    /// from <see cref="IMessageContext"/> via <see cref="SqLiteHeaders.ConnectionState"/>
    /// (PLAN-1.1 Approach B — context-state-based pattern). The receive path stores the
    /// state on context in <c>ReceiveMessage.GetMessage</c>; this class reads it back.
    /// </para>
    /// </remarks>
    internal class SqLiteRelationalWorkerNotification : WorkerNotification, IRelationalWorkerNotification
    {
        private readonly IMessageContext _context;
        private readonly SqLiteHeaders _sqLiteHeaders;

        /// <summary>
        /// Initializes a new instance of <see cref="SqLiteRelationalWorkerNotification"/>.
        /// The six base parameters are forwarded unchanged to <see cref="WorkerNotification"/>;
        /// SimpleInjector resolves them from the container with no additional plumbing.
        /// </summary>
        /// <param name="headerNames">The header names.</param>
        /// <param name="cancelWork">The cancel work.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="log">The log.</param>
        /// <param name="metrics">The metrics factory.</param>
        /// <param name="tracer">The tracer.</param>
        /// <param name="context">The per-message context — carries the hold-tx state set by the receive path.</param>
        /// <param name="sqLiteHeaders">Typed key resolver for reading the connection state off the context.</param>
        public SqLiteRelationalWorkerNotification(
            IHeaders headerNames,
            IQueueCancelWork cancelWork,
            TransportConfigurationReceive configuration,
            ILogger log,
            IMetrics metrics,
            ActivitySource tracer,
            IMessageContext context,
            SqLiteHeaders sqLiteHeaders)
            : base(headerNames, cancelWork, configuration, log, metrics, tracer)
        {
            _context = context;
            _sqLiteHeaders = sqLiteHeaders;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Reads the per-message <see cref="SqLiteConnectionState"/> from the context via
        /// <see cref="SqLiteHeaders.ConnectionState"/>. When the state is present (option
        /// is true AND a message has been dequeued AND the receive path has set the state),
        /// returns its <see cref="System.Data.IDbTransaction"/> upcast to <see cref="DbTransaction"/>.
        /// Returns null only between construction and the receive path's state-set call;
        /// never null during user-handler execution when this class is in scope.
        /// </remarks>
        public DbTransaction Transaction
        {
            get
            {
                var state = _context?.Get(_sqLiteHeaders.ConnectionState);
                return state?.Transaction as DbTransaction;
            }
        }
    }
}
