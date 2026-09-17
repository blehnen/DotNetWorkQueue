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
    /// The queue a producer or consumer was pointed at has not been created.
    /// </summary>
    /// <remarks>
    /// Raised when the queue is built, rather than left to surface later, because later is not legible:
    /// on SQL Server and PostgreSQL the first de-queue failed with the transport's own error and the
    /// consumer recovered only if the queue appeared, and on SQLite and LiteDb it never recovered at all -
    /// LiteDb without so much as a logged error, leaving a consumer that looked healthy and processed
    /// nothing for as long as it ran (GitHub #348).
    /// </remarks>
    public class QueueDoesNotExistException : DotNetWorkQueueException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueDoesNotExistException"/> class.
        /// </summary>
        /// <param name="queueName">The queue that does not exist.</param>
        public QueueDoesNotExistException(string queueName)
            : base($"The queue {queueName} does not exist. Create it before creating a producer or consumer for it.")
        {
            QueueName = queueName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueDoesNotExistException"/> class.
        /// </summary>
        public QueueDoesNotExistException() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueDoesNotExistException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The exception that caused this one.</param>
        public QueueDoesNotExistException(string message, Exception inner) : base(message, inner) { }

        /// <summary>
        /// The queue that does not exist.
        /// </summary>
        public string QueueName { get; }
    }
}
