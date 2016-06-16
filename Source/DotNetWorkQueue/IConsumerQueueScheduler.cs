// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
namespace DotNetWorkQueue
{
    /// <summary>
    /// A task scheduler queue that runs on top of a <see cref="IConsumerQueueAsync"/> instance.
    /// </summary>
    /// <remarks>Use this queue to manage the tasks for you. Use <see cref="IConsumerQueueAsync"/> if you want to manage the tasks yourself.</remarks>
    public interface IConsumerQueueScheduler : IConsumerBaseQueue
    {
        /// <summary>
        /// Starts the queue
        /// </summary>
        /// <param name="functionToRun">The function to run to handle messages.</param>
        void Start<T>(Action<IReceivedMessage<T>, IWorkerNotification> functionToRun)
            where T : class;
    }
}
