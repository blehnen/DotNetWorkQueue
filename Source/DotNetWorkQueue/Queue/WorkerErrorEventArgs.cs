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
using System;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Holds information about an error that a worker has encountered
    /// </summary>
    public class WorkerErrorEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WorkerErrorEventArgs"/> class.
        /// </summary>
        /// <param name="worker">The worker.</param>
        /// <param name="error">The error.</param>
        public WorkerErrorEventArgs(IWorkerBase worker, Exception error)
        {
            Worker = worker;
            Error = error;
        }
        /// <summary>
        /// Gets the worker.
        /// </summary>
        /// <value>
        /// The worker.
        /// </value>
        public IWorkerBase Worker { get; }
        /// <summary>
        /// Gets the error.
        /// </summary>
        /// <value>
        /// The error.
        /// </value>
        public Exception Error { get;  }
    }
}
