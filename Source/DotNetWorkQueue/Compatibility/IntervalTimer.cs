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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetWorkQueue.Compatibility
{
    /// <summary>
    /// Ticks on a fixed cadence, for the targets whose framework has no <c>PeriodicTimer</c>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The property that matters is that the cadence is measured from a fixed point rather than from
    /// when the caller finished its work. Delaying for the period after each iteration looks
    /// equivalent and is not: the work's duration is added to every interval, so a loop whose body
    /// occasionally takes longer than a period falls behind and stays behind. That is the defect
    /// #303 chased and #315 fixed by moving the heartbeat onto a real timer, and a shim that
    /// reintroduced it would put the bug back on one target only, where it would be hardest to see.
    /// </para>
    /// <para>
    /// A tick missed because the caller was slow is dropped rather than queued, which is what
    /// <c>PeriodicTimer</c> does. Beats are a statement about now, so firing three in a row to catch
    /// up would be worse than skipping them.
    /// </para>
    /// <para>
    /// Time comes from <see cref="Stopwatch"/> rather than the clock, so adjusting the machine's
    /// time does not stall or stampede the loop. This is compiled on every target, not only the one
    /// that needs it, so the tests cover it - a type used only under netstandard2.0 would never be
    /// exercised by a suite that runs on net10.0 (GitHub #252).
    /// </para>
    /// </remarks>
    internal sealed class IntervalTimer : IDisposable
    {
        private readonly long _periodTicks;
        private readonly CancellationTokenSource _disposed = new CancellationTokenSource();
        private long _nextDueTicks;
        private int _disposeCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="IntervalTimer"/> class.
        /// </summary>
        /// <param name="period">How often to tick. Must be positive.</param>
        public IntervalTimer(TimeSpan period)
        {
            if (period <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(period), period, "The period must be positive.");

            _periodTicks = (long)(period.TotalSeconds * Stopwatch.Frequency);
            if (_periodTicks <= 0)
                _periodTicks = 1;

            _nextDueTicks = Stopwatch.GetTimestamp() + _periodTicks;
        }

        /// <summary>
        /// Waits for the next tick.
        /// </summary>
        /// <param name="cancellation">Cancels the wait.</param>
        /// <returns>True on a tick; false once the timer has been disposed.</returns>
        public async ValueTask<bool> WaitForNextTickAsync(CancellationToken cancellation)
        {
            if (Volatile.Read(ref _disposeCount) != 0)
                return false;

            CancellationTokenSource linked;
            try
            {
                linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation, _disposed.Token);
            }
            catch (ObjectDisposedException)
            {
                //Dispose ran between the check above and this line, so reading its token threw.
                //The contract is that disposal ends the wait with false; throwing here would
                //instead fault the caller's loop at exactly the moment it is shutting down.
                return false;
            }

            using (linked)
            {
                while (true)
                {
                    var remaining = _nextDueTicks - Stopwatch.GetTimestamp();
                    if (remaining <= 0)
                        break;

                    var wait = TimeSpan.FromSeconds((double)remaining / Stopwatch.Frequency);
                    try
                    {
                        await Task.Delay(wait, linked.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        //disposal ends the loop quietly; a caller's cancellation is theirs to see
                        if (Volatile.Read(ref _disposeCount) != 0)
                            return false;
                        throw;
                    }
                }
            }

            if (Volatile.Read(ref _disposeCount) != 0)
                return false;

            AdvanceToNextDue();
            return true;
        }

        /// <summary>
        /// Moves the due time forward by whole periods until it is in the future.
        /// </summary>
        /// <remarks>
        /// Advancing by one period would let the due time fall further behind on every slow
        /// iteration, and the loop would then tick continuously trying to catch up. Skipping whole
        /// periods keeps the cadence aligned to the original start and drops what was missed.
        /// </remarks>
        private void AdvanceToNextDue()
        {
            var now = Stopwatch.GetTimestamp();
            do
            {
                _nextDueTicks += _periodTicks;
            }
            while (_nextDueTicks <= now);
        }

        public void Dispose()
        {
            if (Interlocked.Increment(ref _disposeCount) != 1)
                return;

            _disposed.Cancel();
            _disposed.Dispose();
        }
    }
}
