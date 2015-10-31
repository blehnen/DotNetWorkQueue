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
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
namespace DotNetWorkQueue
{
    /// <summary>
    /// Defines a queue that can process messages in an async fashion.
    /// </summary>
    public interface IConsumerQueueAsync : IDisposable, IIsDisposed
    {
        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        QueueConsumerConfiguration Configuration { get; }

        /// <summary>
        /// Starts the queue.
        /// </summary>
        /// <typeparam name="T">The type of the message</typeparam>
        /// <param name="workerAction">The worker action.</param>
        /// <remarks>Call dispose to stop the queue once started</remarks>
        void Start<T>(Func<IReceivedMessage<T>, IWorkerNotification, Task> workerAction)
            where T : class;
    }
}
