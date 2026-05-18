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
using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// SQLite-transport-specific typed keys for storing per-message state on
    /// <see cref="IMessageContext"/>. Registered as a singleton in
    /// <c>SqLiteMessageQueueSharedInit</c>.
    /// </summary>
    /// <remarks>
    /// Phase 5 introduced this header for the hold-transaction implementation
    /// (<see cref="ConnectionState"/>). Parallel to SqlServer's
    /// <see cref="DotNetWorkQueue.Transport.RelationalDatabase.IConnectionHeader{TConnection,TTransaction,TCommand}"/>
    /// usage, but using a SQLite-specific state type rather than the shared typed
    /// connection holder.
    /// </remarks>
    internal sealed class SqLiteHeaders
    {
        /// <summary>
        /// Typed key for the per-message <see cref="SqLiteConnectionState"/>. Set on the
        /// context in the receive path when
        /// <c>EnableHoldTransactionUntilMessageCommitted</c> is true; read by the commit /
        /// rollback / cleanup delegates and by the inbox relational worker notification.
        /// </summary>
        public IMessageContextData<SqLiteConnectionState> ConnectionState { get; } =
            new MessageContextData<SqLiteConnectionState>("SqLite.ConnectionState", null);
    }
}
