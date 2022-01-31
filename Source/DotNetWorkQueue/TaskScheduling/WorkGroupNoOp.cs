// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
namespace DotNetWorkQueue.TaskScheduling
{
    /// <inheritdoc />
    /// <summary>
    /// A NoOp work group
    /// </summary>
    public class WorkGroupNoOp : IWorkGroup
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name => string.Empty;

        /// <summary>
        /// Gets the concurrency level.
        /// </summary>
        /// <value>
        /// The concurrency level.
        /// </value>
        public int ConcurrencyLevel => 0;

        /// <summary>
        /// Gets the maximum size of the in memory queue for this work item
        /// </summary>
        /// <value>
        /// The maximum size of the queue.
        /// </value>
        public int MaxQueueSize => 0;
    }
}
