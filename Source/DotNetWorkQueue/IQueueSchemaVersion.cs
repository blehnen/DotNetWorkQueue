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

namespace DotNetWorkQueue
{
    /// <summary>
    /// A transport whose queues carry a schema version, and can be upgraded in place.
    /// </summary>
    /// <remarks>
    /// Optional. A transport that publishes this is saying its queues have a schema that can drift
    /// from the library, and that an existing queue can be brought forward without losing its data.
    /// Transports with no schema to drift - Redis, and LiteDb today - do not publish it, and nothing
    /// checks a version for them.
    ///
    /// Deliberately separate from <see cref="IQueueCreation"/> rather than added to it. Extending that
    /// interface without breaking every implementer means default interface members, and those are the
    /// blocker for bringing back a .NET Framework target (GitHub #252). Resolving an optional interface
    /// costs nothing and keeps that door open.
    /// </remarks>
    public interface IQueueSchemaVersion
    {
        /// <summary>
        /// The schema version the queue is currently at.
        /// </summary>
        /// <remarks>
        /// Zero means the queue was created before schema versioning existed. That is a normal state
        /// rather than an error, and is how every queue created up to now reads.
        /// </remarks>
        long CurrentSchemaVersion { get; }

        /// <summary>
        /// The schema version this build of the library expects.
        /// </summary>
        long TargetSchemaVersion { get; }

        /// <summary>
        /// Brings the queue's schema up to <see cref="TargetSchemaVersion"/>, preserving its data.
        /// </summary>
        /// <remarks>
        /// Explicit on purpose. The upgrade mutates live data and its failure mode is quiet, so it is
        /// not something a routine call should trigger as a side effect. Producers and consumers refuse
        /// to start against an out-of-date queue, so the upgrade runs with nothing else attached to it.
        ///
        /// Safe to call from more than one process at once: the first takes a lock and applies, and the
        /// others find the work already done.
        /// </remarks>
        /// <returns>The outcome, including the versions moved between.</returns>
        SchemaUpgradeResult UpgradeSchema();
    }
}
