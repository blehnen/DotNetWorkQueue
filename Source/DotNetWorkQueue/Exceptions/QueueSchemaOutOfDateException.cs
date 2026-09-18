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

namespace DotNetWorkQueue.Exceptions
{
    /// <summary>
    /// The queue a producer or consumer was pointed at has an older schema than this library expects.
    /// </summary>
    /// <remarks>
    /// The library refuses rather than working against the older schema on purpose. Supporting both
    /// means every fix ships a compatibility branch and keeps it for as long as anyone might not have
    /// upgraded, and those branches are the least exercised code in the transport. Refusing keeps that
    /// cost at zero and lets the existing ones be removed (GitHub #308).
    ///
    /// Raised where <see cref="QueueDoesNotExistException"/> is raised, and for the same reason: queue
    /// creation is the last point the caller can still do something about it.
    /// </remarks>
    public class QueueSchemaOutOfDateException : DotNetWorkQueueException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueSchemaOutOfDateException"/> class.
        /// </summary>
        /// <param name="queueName">The queue whose schema is out of date.</param>
        /// <param name="currentVersion">The schema version the queue is at.</param>
        /// <param name="targetVersion">The schema version this library expects.</param>
        public QueueSchemaOutOfDateException(string queueName, long currentVersion, long targetVersion)
            : base($"The schema for queue {queueName} is at version {currentVersion}, but this version of " +
                   $"DotNetWorkQueue expects version {targetVersion}. Upgrade the schema before creating a " +
                   "producer or consumer for it. Upgrading preserves the queue's data, including history " +
                   "and the error queue; re-creating the queue does not.")
        {
            QueueName = queueName;
            CurrentVersion = currentVersion;
            TargetVersion = targetVersion;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueSchemaOutOfDateException"/> class.
        /// </summary>
        public QueueSchemaOutOfDateException() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueSchemaOutOfDateException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The exception that caused this one.</param>
        public QueueSchemaOutOfDateException(string message, Exception inner) : base(message, inner) { }

        /// <summary>
        /// The queue whose schema is out of date.
        /// </summary>
        public string QueueName { get; }

        /// <summary>
        /// The schema version the queue is at. Zero means the queue predates schema versioning.
        /// </summary>
        public long CurrentVersion { get; }

        /// <summary>
        /// The schema version this library expects.
        /// </summary>
        public long TargetVersion { get; }
    }
}
