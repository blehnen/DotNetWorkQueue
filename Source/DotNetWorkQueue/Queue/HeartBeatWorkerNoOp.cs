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

using System;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// A NoOp implementation of <see cref="IHeartBeatWorker"/>
    /// </summary>
    public class HeartBeatWorkerNoOp : IHeartBeatWorker, INoOperation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HeartBeatWorkerNoOp"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        public HeartBeatWorkerNoOp(IMessageContext context)
        {
            context.WorkerNotification.HeartBeat = new WorkerHeartBeatNotificationNoOp();
        }
        /// <summary>
        /// Starts this instance.
        /// </summary>
        public void Start()
        {
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        /// <remarks>
        /// Stop is explicitly called when an error occurs, so that we can preserve the last heartbeat value.
        /// Implementations MUST ensure that stop blocks and does not return if the heartbeat is in the middle of updating.
        /// </remarks>
        public void Stop()
        {

        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                IsDisposed = true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
        public bool IsDisposed { get; private set; }
    }
}
