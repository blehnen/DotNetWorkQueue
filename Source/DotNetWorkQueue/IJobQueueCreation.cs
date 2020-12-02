// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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
using DotNetWorkQueue.Configuration;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Creates a queue in a transport that can store re-occurring jobs.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    /// <seealso cref="DotNetWorkQueue.IIsDisposed" />
    public interface IJobQueueCreation : IDisposable, IIsDisposed
    {
        /// <summary>
        /// Tells the transport to setup and create a queue for handling re-occurring jobs.
        /// </summary>
        /// <param name="registerService">The additional registrations.</param>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="enableRoutes">if set to <c>true</c> route support will be enabled.</param>
        /// <param name="setOptions">the options to set on the scheduler queue</param>
        /// <returns></returns>
        QueueCreationResult CreateJobSchedulerQueue(Action<IContainer> registerService, QueueConnection queueConnection, Action<IContainer> setOptions = null, bool enableRoutes = false);

        /// <summary>
        /// Attempts to delete an existing queue
        /// </summary>
        /// <remarks>May not be supported by all transports. Any data in the queue will be lost.</remarks>
        /// <returns></returns>
        QueueRemoveResult RemoveQueue();

        /// <summary>
        /// Gets a disposable creation scope
        /// </summary>
        /// <value>
        /// The scope.
        /// </value>
        /// <remarks>This is used to prevent queues from going out of scope before you have finished working with them. Generally
        /// speaking this only matters for queues that live in-memory. However, a valid object is always returned.</remarks>
        ICreationScope Scope { get; }
    }
}
