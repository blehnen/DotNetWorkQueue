// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
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
    /// An internal thread pool
    /// </summary>
    public interface IThreadPool: IDisposable, IIsDisposed
    {
        /// <summary>
        /// Gets a value indicating whether this instance is shutting down.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is shutting down; otherwise, <c>false</c>.
        /// </value>
        bool IsShuttingdown { get; }
        /// <summary>
        /// Queues a work item.
        /// </summary>
        /// <param name="action">The work item to queue.</param>
        void QueueWorkItem(Action action);
        /// <summary>
        /// Gets the active threads count.
        /// </summary>
        /// <value>
        /// The active threads.
        /// </value>
        int ActiveThreads { get; }
        /// <summary>
        /// Gets a value indicating whether this instance is started.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is started; otherwise, <c>false</c>.
        /// </value>
        bool IsStarted { get; }
        /// <summary>
        /// Starts this instance.
        /// </summary>
        void Start();
    }
}
