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
    /// one.
    /// </para>
    /// </remarks>
    internal static class ScheduledJobLock
    {
        private static readonly object Instance = new object();

        /// <summary>
        /// Holds the lock for the duration of the returned scope when <paramref name="jobName"/>
        /// names a job, and does nothing otherwise.
        /// </summary>
        /// <param name="jobName">The job name, or null/blank for an ordinary message.</param>
        public static Holder AcquireIfJob(string jobName) =>
            new Holder(string.IsNullOrWhiteSpace(jobName) ? null : Instance);

        /// <summary>
        /// A scope that releases the lock if it took one. A struct, so an ordinary send allocates
        /// nothing to decide it needs no lock.
        /// </summary>
        internal struct Holder : IDisposable
        {
            private readonly object _lockObject;
            private bool _taken;

            internal Holder(object lockObject)
            {
                _lockObject = lockObject;
                _taken = false;
                if (lockObject != null)
                    Monitor.Enter(lockObject, ref _taken);
            }

            /// <inheritdoc />
            public void Dispose()
            {
                if (!_taken) return;
                _taken = false;
                Monitor.Exit(_lockObject);
            }
        }
    }
}
