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

namespace DotNetWorkQueue.Transport.Shared.Basic.Command
{
    /// <summary>
    /// Sends a heart beat to a queue record.
    /// </summary>
    public class SendHeartBeatCommand<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SendHeartBeatCommand{T}"/> class.
        /// </summary>
        /// <param name="queueId">The queue identifier.</param>
        public SendHeartBeatCommand(T queueId)
            : this(queueId, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SendHeartBeatCommand{T}"/> class.
        /// </summary>
        /// <param name="queueId">The queue identifier.</param>
        /// <param name="previousHeartBeat">The heartbeat this caller last wrote, or null if it has not
        /// written one yet.</param>
        public SendHeartBeatCommand(T queueId, DateTime? previousHeartBeat)
        {
            QueueId = queueId;
            PreviousHeartBeat = previousHeartBeat;
        }

        /// <summary>
        /// The heartbeat value this caller last wrote, used to prove the claim is still its own.
        /// </summary>
        /// <remarks>
        /// A heartbeat update names only the message, so on its own it cannot tell "my claim" from "the
        /// claim someone else now holds on the same message". Once the monitor has reset a message and
        /// another worker has taken it, a stale worker's update would match the row again and refresh a
        /// claim belonging to somebody else, and both would carry on processing (GitHub #328).
        ///
        /// Matching on the value we expect to replace closes that: the monitor resets the heartbeat to
        /// null and the new owner writes its own, so either way the value differs from ours and the
        /// update stops matching. Null means no beat has been written yet, which is the state a message
        /// is dequeued in - and the monitor ignores rows whose heartbeat is null, so nothing can reset
        /// it before the first beat lands.
        /// </remarks>
        public DateTime? PreviousHeartBeat { get; }
        /// <summary>
        /// Gets the queue identifier.
        /// </summary>
        /// <value>
        /// The queue identifier.
        /// </value>
        public T QueueId { get; }
    }
}
