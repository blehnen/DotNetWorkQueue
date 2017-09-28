// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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

using DotNetWorkQueue.Validation;
using Polly.Bulkhead;

namespace DotNetWorkQueue.TaskScheduling
{
    /// <summary>
    /// Stores the work group along with the smart thread pool work items group
    /// </summary>
    internal class WorkGroupWithItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WorkGroupWithItem"/> class.
        /// </summary>
        /// <param name="sourceGroup">The source group.</param>
        /// <param name="threadGroup">The thread group.</param>
        /// <param name="metricCounter">A counter for tracking how many items are being processed</param>
        public WorkGroupWithItem(IWorkGroup sourceGroup, BulkheadPolicy threadGroup, ICounter metricCounter)
        {
            Guard.NotNull(() => sourceGroup, sourceGroup);
            Guard.NotNull(() => threadGroup, threadGroup);
            Guard.NotNull(() => metricCounter, metricCounter);

            GroupInfo = sourceGroup;
            Group = threadGroup;
            MaxWorkItems = GroupInfo.ConcurrencyLevel + GroupInfo.MaxQueueSize;
            MetricCounter = metricCounter;
        }
        /// <summary>
        /// Gets or sets the group information.
        /// </summary>
        /// <value>
        /// The group information.
        /// </value>
        public IWorkGroup GroupInfo { get; }
        /// <summary>
        /// Gets or sets the group.
        /// </summary>
        /// <value>
        /// The group.
        /// </value>
        public BulkheadPolicy Group { get;  }
        /// <summary>
        /// How many work items are being processed and are also in the queue
        /// </summary>
        /// <remarks>This cannot be a property, as it's updated via interlock</remarks>
        public int CurrentWorkItems;
        /// <summary>
        /// Gets or sets the maximum work items.
        /// </summary>
        /// <value>
        /// The maximum work items.
        /// </value>
        public int MaxWorkItems { get; }
        /// <summary>
        /// Records how many items are currently being processed
        /// </summary>
        public ICounter MetricCounter { get;  }
    }
}
