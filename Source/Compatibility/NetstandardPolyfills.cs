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
#if NETSTANDARD2_0
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetWorkQueue.Compatibility
{
    /// <summary>
    /// Framework methods that netstandard2.0 does not have, under the names the rest of the code
    /// already calls them by.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>Directory.Build.props</c> links this file into each netstandard2.0 build and puts this
    /// namespace in scope globally there, so the call sites need no using directive and stay
    /// identical on every target. Linked rather than referenced because an internal extension
    /// method has to live in the assembly that uses it (GitHub #252).
    /// </para>
    /// <para>
    /// The asynchronous ones run the synchronous work and hand back a completed task. That is not
    /// sync-over-async - there is nothing asynchronous underneath to block on - it is the shape of
    /// an API that is synchronous on this framework, where the base classes have no asynchronous
    /// member for a provider to override. Callers therefore get the same behaviour a provider
    /// without an override gives them on any target.
    /// </para>
    /// </remarks>
    internal static class NetstandardPolyfills
    {
        /// <summary>Stands in for <c>DbConnection.BeginTransactionAsync</c> (.NET Core 3.0).</summary>
        public static Task<DbTransaction> BeginTransactionAsync(this DbConnection connection,
            CancellationToken cancellation = default)
        {
            cancellation.ThrowIfCancellationRequested();
            return Task.FromResult(connection.BeginTransaction());
        }

        /// <summary>Stands in for <c>DbTransaction.CommitAsync</c> (.NET Core 3.0).</summary>
        public static Task CommitAsync(this DbTransaction transaction,
            CancellationToken cancellation = default)
        {
            cancellation.ThrowIfCancellationRequested();
            transaction.Commit();
            return Task.CompletedTask;
        }

        /// <summary>Stands in for <c>DbTransaction.RollbackAsync</c> (.NET Core 3.0).</summary>
        public static Task RollbackAsync(this DbTransaction transaction,
            CancellationToken cancellation = default)
        {
            cancellation.ThrowIfCancellationRequested();
            transaction.Rollback();
            return Task.CompletedTask;
        }

        /// <summary>Stands in for <c>DbTransaction.DisposeAsync</c> (.NET Core 3.0).</summary>
        public static ValueTask DisposeAsync(this DbTransaction transaction)
        {
            transaction.Dispose();
            return default;
        }

        /// <summary>Stands in for <c>DbCommand.DisposeAsync</c> (.NET Core 3.0).</summary>
        public static ValueTask DisposeAsync(this DbCommand command)
        {
            command.Dispose();
            return default;
        }

        /// <summary>Stands in for <c>CancellationTokenSource.CancelAsync</c> (.NET 8).</summary>
        /// <remarks>
        /// The framework method exists so registered callbacks do not run on the caller's thread.
        /// Here they do, which is what every caller of <c>Cancel</c> on this framework already
        /// lives with.
        /// </remarks>
        public static Task CancelAsync(this CancellationTokenSource source)
        {
            source.Cancel();
            return Task.CompletedTask;
        }
    }
}
#endif
