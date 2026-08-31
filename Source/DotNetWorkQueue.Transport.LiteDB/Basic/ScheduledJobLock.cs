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

namespace DotNetWorkQueue.Transport.LiteDb.Basic
{
    /// <summary>
    /// The critical section that keeps a scheduled job from being queued twice.
    /// </summary>
    /// <remarks>
    /// Queueing a job is a check-then-act — "is this job already queued" followed by the insert —
    /// and LiteDB's <c>BeginTrans</c> does not block in direct or memory mode, so two producers can
    /// both read <c>NotQueued</c> before either commits.
    /// <para>
    /// One object, shared by the synchronous and asynchronous handlers. They previously held a
    /// static lock <em>each</em>, so a synchronous send and an asynchronous send of the same job
    /// excluded others of their own kind but never each other.
    /// </para>
    /// <para>
    /// An ordinary send takes nothing at all: its inserts are covered by the transaction, and
    /// taking a process-wide lock for every message made concurrent producers slower than a single
    /// one. It also allocates nothing — the no-op scope is a single cached instance.
    /// </para>
    /// </remarks>
    internal static class ScheduledJobLock
    {
        private static readonly object Instance = new object();

        /// <summary>
        /// The scope handed back for an ordinary send. Stateless, so one instance serves every
        /// caller on every thread and the common path allocates nothing.
        /// </summary>
        private static readonly IDisposable NotAJob = new NoScope();

        /// <summary>
        /// Holds the lock until the returned scope is disposed when <paramref name="jobName"/>
        /// names a job, and does nothing otherwise.
        /// </summary>
        /// <param name="jobName">The job name, or null/blank for an ordinary message.</param>
        /// <returns>A scope to dispose; never null.</returns>
        public static IDisposable AcquireIfJob(string jobName) =>
            string.IsNullOrWhiteSpace(jobName) ? NotAJob : new JobScope(Instance);

        /// <summary>Does nothing, for the sends that need no critical section.</summary>
        private sealed class NoScope : IDisposable
        {
            public void Dispose()
            {
                //nothing was taken, so there is nothing to release
            }
        }

        /// <summary>
        /// Holds the lock for one scheduled send.
        /// </summary>
        /// <remarks>
        /// A class rather than a struct on purpose. A struct scope can be copied, and every copy
        /// would carry the "I hold the lock" flag — disposing two copies calls
        /// <see cref="Monitor.Exit"/> twice, and the second throws
        /// <see cref="SynchronizationLockException"/>. A reference type has one state no matter how
        /// many times it is passed around, and the interlocked guard makes a second Dispose a no-op
        /// rather than an exception. Only a scheduled send allocates one, and those are rare.
        /// </remarks>
        private sealed class JobScope : IDisposable
        {
            private readonly object _lockObject;
            private int _held;

            internal JobScope(object lockObject)
            {
                _lockObject = lockObject;
                var taken = false;
                Monitor.Enter(lockObject, ref taken);
                _held = taken ? 1 : 0;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                //release once and only once, whoever calls and however often
                if (Interlocked.Exchange(ref _held, 0) == 1)
                    Monitor.Exit(_lockObject);
            }
        }
    }
}
