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
using System.Collections.Concurrent;

namespace DotNetWorkQueue.Transport.LiteDb.Basic
{
    /// <summary>
    /// Locks that serialize access to one LiteDb database, rather than to every database in the
    /// process.
    /// </summary>
    /// <remarks>
    /// The send and receive paths each need a critical section, but only against themselves and
    /// only per database. They previously used <c>static readonly object</c> fields, which made
    /// every queue in the process contend on one lock: measured at four producer threads running
    /// 1.35x <em>slower</em> than one, and two queues with separate database files performing no
    /// better than a single queue. LiteDB's own locking allowed the same inserts on four threads in
    /// well under half that time, so the ceiling was this library's rather than the storage
    /// engine's.
    /// <para>
    /// The two lock sets are separate because they guard unrelated things - a dequeue does not need
    /// to wait behind a job being queued - and sharing one would reintroduce contention that has no
    /// reason to exist.
    /// </para>
    /// <para>
    /// Entries are never removed. A lock cannot be evicted safely while a thread might be waiting
    /// on it, and the number of distinct database files a process opens is bounded by the
    /// application rather than by message volume, so the dictionary stays small on its own.
    /// </para>
    /// </remarks>
    internal static class DatabaseLocks
    {
        private static readonly ConcurrentDictionary<string, object> JobLocks =
            new ConcurrentDictionary<string, object>(StringComparer.Ordinal);

        private static readonly ConcurrentDictionary<string, object> DequeueLocks =
            new ConcurrentDictionary<string, object>(StringComparer.Ordinal);

        /// <summary>
        /// The lock guarding the "is this job already queued" check and the insert that follows it.
        /// Needed only when a message carries a job name; an ordinary send is covered by the
        /// transaction alone.
        /// </summary>
        /// <param name="databaseKey">From <see cref="LiteDbConnectionManager.DatabaseKey"/>.</param>
        public static object ForJobs(string databaseKey) =>
            JobLocks.GetOrAdd(databaseKey, static _ => new object());

        /// <summary>
        /// The lock enforcing one de-queue at a time against a database. LiteDB's
        /// <c>BeginTrans</c> does not block in direct or memory mode, so without this two consumers
        /// can claim the same record.
        /// </summary>
        /// <param name="databaseKey">From <see cref="LiteDbConnectionManager.DatabaseKey"/>.</param>
        public static object ForDequeue(string databaseKey) =>
            DequeueLocks.GetOrAdd(databaseKey, static _ => new object());
    }
}
