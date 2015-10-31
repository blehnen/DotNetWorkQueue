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
using DotNetWorkQueue.Queue;
namespace DotNetWorkQueue
{
    /// <summary>
    /// The base worker class for processing messages.
    /// </summary>
    public interface IWorkerBase: IDisposable, IIsDisposed
    {
        /// <summary>
        /// Event that will be raised each time message delivery fails.
        /// </summary>
        event EventHandler<WorkerErrorEventArgs> UserException;

        /// <summary>
        /// Event that will be raised if an exception occurs outside of user code.
        /// </summary>
        event EventHandler<WorkerErrorEventArgs> SystemException;

        /// <summary>
        /// Gets a value indicating whether this <see cref="IWorkerBase"/> is running.
        /// </summary>
        /// <value>
        ///   <c>true</c> if running; otherwise, <c>false</c>.
        /// </value>
        bool Running { get; }
        /// <summary>
        /// Starts this instance.
        /// </summary>
        void Start();
        /// <summary>
        /// Stops this instance.
        /// </summary>
        void Stop();
        /// <summary>
        /// Forces the worker to terminate.
        /// <remarks>This method should not return until the worker has shutdown.</remarks>
        /// </summary>
        void TryForceTerminate();
        /// <summary>
        /// Attempts to terminate the worker.
        /// </summary>
        /// <remarks>Will return false if the worker is still running</remarks>
        /// <returns>true if terminated, false if not</returns>
        bool AttemptToTerminate();
    }
}
