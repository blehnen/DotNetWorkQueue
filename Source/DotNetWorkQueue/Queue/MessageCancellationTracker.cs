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
using System.Collections.Concurrent;
using System.Threading;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Tracks per-message <see cref="CancellationTokenSource"/> instances and provides
    /// cancellation support via <see cref="ICancelRunningMessage"/>.
    /// Uses a static dictionary so the tracker is shared across containers in the same process
    /// (consumer container and admin/dashboard container).
    /// </summary>
    public class MessageCancellationTracker : ICancelRunningMessage
    {
        private static readonly ConcurrentDictionary<string, CancellationTokenSource> Tokens
            = new ConcurrentDictionary<string, CancellationTokenSource>();

        /// <summary>
        /// Creates a linked <see cref="CancellationTokenSource"/> for the given message,
        /// linked to the worker-level cancellation tokens so either can trigger cancellation.
        /// </summary>
        /// <param name="queueId">The message's queue ID.</param>
        /// <param name="workerTokens">The worker-level cancellation tokens to link with.</param>
        /// <returns>The linked cancellation token for this message.</returns>
        public CancellationToken Register(string queueId, params CancellationToken[] workerTokens)
        {
            var cts = workerTokens != null && workerTokens.Length > 0
                ? CancellationTokenSource.CreateLinkedTokenSource(workerTokens)
                : new CancellationTokenSource();
            Tokens[queueId] = cts;
            return cts.Token;
        }

        /// <summary>
        /// Removes and disposes the <see cref="CancellationTokenSource"/> for the given message.
        /// Called when message processing completes (commit, error, rollback).
        /// </summary>
        /// <param name="queueId">The message's queue ID.</param>
        public void Unregister(string queueId)
        {
            if (Tokens.TryRemove(queueId, out var cts))
            {
                cts.Dispose();
            }
        }

        /// <inheritdoc />
        public bool Cancel(string queueId)
        {
            if (Tokens.TryGetValue(queueId, out var cts) && !cts.IsCancellationRequested)
            {
                cts.Cancel();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns true if any messages are currently being tracked (i.e., consumers are running in-process).
        /// Used by the dashboard to determine if cancellation is available.
        /// </summary>
        public static bool HasActiveConsumers => !Tokens.IsEmpty;

        /// <summary>
        /// Returns true if the specified message is currently being processed.
        /// </summary>
        public static bool IsProcessing(string queueId) => Tokens.ContainsKey(queueId);
    }
}
