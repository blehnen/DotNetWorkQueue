// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2018 Brian Lehnen
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
namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command
{
    /// <summary>
    /// Updates the status of a record in the status table of a queue
    /// </summary>
    public class SetStatusTableStatusCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SetStatusTableStatusCommand" /> class.
        /// </summary>
        /// <param name="queueId">The queue identifier.</param>
        /// <param name="status">The status.</param>
        public SetStatusTableStatusCommand(long queueId, QueueStatuses status)
        {
            QueueId = queueId;
            Status = status;
        }
        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        public QueueStatuses Status { get; }
        /// <summary>
        /// Gets the queue identifier.
        /// </summary>
        /// <value>
        /// The queue identifier.
        /// </value>
        public long QueueId { get; }
    }
}
