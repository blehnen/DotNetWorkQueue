// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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
    /// Indicates the root type of object that this scope contains
    /// </summary>
    public enum QueueContexts
    {
        /// <summary>
        /// The root type was not specified
        /// </summary>
        NotSet = 0,
        /// <summary>
        /// The root type is not known
        /// </summary>
        Unknown = 1,
        /// <summary>
        /// The consumer queue
        /// </summary>
        ConsumerQueue = 2,
        /// <summary>
        /// An async consumer queue
        /// </summary>
        ConsumerQueueAsync = 3,
        /// <summary>
        /// The queue/scheduler for an async consumer queue
        /// </summary>
        ConsumerQueueScheduler = 4,
        /// <summary>
        /// The producer queue
        /// </summary>
        ProducerQueue = 5,
        /// <summary>
        /// The task scheduler
        /// </summary>
        TaskScheduler = 8,
        /// <summary>
        /// The task factory
        /// </summary>
        TaskFactory = 9,
        /// <summary>
        /// The queue creator module
        /// </summary>
        QueueCreator = 10,
        /// <summary>
        /// The queue status module
        /// </summary>
        QueueStatus = 11,
        /// <summary>
        /// The producer method queue
        /// </summary>
        ProducerMethodQueue = 12,
        /// <summary>
        /// The consumer method queue
        /// </summary>
        ConsumerMethodQueue = 13,
        /// <summary>
        /// The queue/scheduler for an async consumer queue; will process method messages
        /// </summary>
        ConsumerMethodQueueScheduler = 14,
        /// <summary>
        /// The job queue creator
        /// </summary>
        JobQueueCreator = 16,
        /// <summary>
        /// The job scheduler. Used to schedule re-occurring tasks
        /// </summary>
        JobScheduler = 17,
        /// <summary>
        /// Returns time from
        /// </summary>
        Time = 18
    }
}
