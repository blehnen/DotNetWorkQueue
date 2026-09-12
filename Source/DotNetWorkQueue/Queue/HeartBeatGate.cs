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
using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Bounds how many heartbeat updates a consumer has in flight at once.
    /// </summary>
    /// <remarks>
    /// A beat is an awaited transport call, so this bounds outstanding operations rather than threads.
    /// A beat that cannot get a slot waits; it is never dropped. Dropping is what costs a message its
    /// claim, and the wait is short - a beat is milliseconds against an interval measured in seconds.
    /// </remarks>
    public sealed class HeartBeatGate : IHeartBeatGate
    {
        private readonly SemaphoreSlim _slots;
        private readonly TimeSpan _warnAfter;
        private readonly ILogger _logger;
        private int _disposeCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="HeartBeatGate"/> class.
        /// </summary>
        /// <param name="configuration">The consumer configuration; the bound and the interval come from it.</param>
        /// <param name="logger">The logger.</param>
        public HeartBeatGate(QueueConsumerConfiguration configuration, ILogger logger)
        {
            Guard.NotNull(configuration);
            Guard.NotNull(logger);

            _logger = logger;
            _warnAfter = configuration.HeartBeat.UpdateTime;

            //Zero means "as many as there are messages in flight", which is the natural ceiling: there
            //is at most one beat per worker. A transport or a caller that wants to throttle the load on
            //its database sets a number instead.
            var max = configuration.HeartBeat.ThreadPoolConfiguration.ThreadsMax;
            if (max <= 0)
                max = Math.Max(1, configuration.Worker.WorkerCount);

            _slots = new SemaphoreSlim(max, max);
        }

        /// <inheritdoc />
        public async Task<IDisposable> EnterAsync(CancellationToken cancellation)
        {
            var waited = System.Diagnostics.Stopwatch.StartNew();
            await _slots.WaitAsync(cancellation).ConfigureAwait(false);
            waited.Stop();

            //Waiting longer than the interval means the next beat is already due - the condition that
            //used to be invisible, because a beat that could not get a thread simply sat in a queue
            if (_warnAfter > TimeSpan.Zero && waited.Elapsed > _warnAfter)
            {
                _logger.LogWarning(
                    "A heartbeat waited {WaitedMs}ms for a slot, longer than the {IntervalMs}ms update interval. " +
                    "Raise HeartBeat.ThreadPoolConfiguration.ThreadsMax, or lower the worker count",
                    waited.Elapsed.TotalMilliseconds, _warnAfter.TotalMilliseconds);
            }

            return new Slot(_slots);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (Interlocked.Increment(ref _disposeCount) != 1) return;
            _slots.Dispose();
        }

        private sealed class Slot : IDisposable
        {
            private readonly SemaphoreSlim _slots;
            private int _released;

            internal Slot(SemaphoreSlim slots)
            {
                _slots = slots;
            }

            public void Dispose()
            {
                if (Interlocked.Increment(ref _released) != 1) return;
                try
                {
                    _slots.Release();
                }
                catch (ObjectDisposedException)
                {
                    //the consumer is going away; there is nothing left to release the slot to
                }
            }
        }
    }
}
