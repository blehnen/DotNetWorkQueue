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
using DotNetWorkQueue.Queue;
namespace DotNetWorkQueue
{
    /// <summary>
    /// A collection of <see cref="IWorker"/>
    /// </summary>
    public interface IWorkerCollection: IDisposable, IIsDisposed
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
        /// Starts all workers.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops all workers.
        /// </summary>
        void Stop();

        /// <summary>
        /// Pauses all workers. Call <seealso cref="ResumeWorkers"/> to resume looking for work.
        /// </summary>
        void PauseWorkers();

        /// <summary>
        /// Resumes all workers. Call <seealso cref="PauseWorkers"/> to pause looking for work
        /// </summary>
        void ResumeWorkers();

        /// <summary>
        /// Returns true if every worker in the collection is set as idle
        /// </summary>
        /// <value>
        ///   <c>true</c> if [all workers are idle]; otherwise, <c>false</c>.
        /// </value>
        bool AllWorkersAreIdle { get; }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        IWorkerConfiguration Configuration { get; }
    }
}
