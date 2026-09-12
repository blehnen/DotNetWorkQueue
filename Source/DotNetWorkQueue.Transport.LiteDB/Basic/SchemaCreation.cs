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
namespace DotNetWorkQueue.Transport.LiteDb.Basic
{
    /// <summary>
    /// Serialises the creation of LiteDb tables across the process.
    /// </summary>
    /// <remarks>
    /// LiteDB resolves <c>EnsureIndex(x =&gt; x.Member)</c> through <see cref="LiteDB.BsonMapper"/>'s
    /// global instance, and mapping a type for the first time is not thread safe: another thread can
    /// see an entity mapper whose member list is not yet populated and report a member that plainly
    /// exists as missing. Two queues being created at once is enough - it was seen on CI, where the
    /// integration tests create queues on four workers, and reproduces on demand with sixty-four
    /// threads. Pre-building the mappers instead was measured and is not sufficient.
    ///
    /// The gate is shared rather than per-handler because the race is on one global mapper: queue
    /// tables and job tables are created by different handlers, and a job queue starting at the same
    /// moment as a message queue maps the same types. Creating either is a rare, one-off operation,
    /// so serialising them costs nothing.
    ///
    /// See GitHub #318.
    /// </remarks>
    internal static class SchemaCreation
    {
        /// <summary>Hold this while creating tables.</summary>
        internal static readonly object Gate = new object();
    }
}
