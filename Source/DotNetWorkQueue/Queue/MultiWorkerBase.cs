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
using System.Threading;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Represents a worker
    /// </summary>
    internal abstract class MultiWorkerBase: WorkerBase
    {
        protected readonly StopThread StopThread;
        protected IMessageProcessing MessageProcessing;
        protected int DisposeCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiWorkerBase" /> class.
        /// </summary>
        /// <param name="workerTerminate">The worker terminate module.</param>
        /// <param name="stopThread">The stop thread.</param>
        protected MultiWorkerBase(WorkerTerminate workerTerminate,
            StopThread stopThread)
            : base(workerTerminate)
        {
            Guard.NotNull(() => workerTerminate, workerTerminate);
            Guard.NotNull(() => StopThread, stopThread);

            StopThread = stopThread;
        }
        /// <summary>
        /// Gets the idle status.
        /// </summary>
        /// <value>
        /// The idle status.
        /// </value>
        public WorkerIdleStatus IdleStatus { get; protected set; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="MultiWorkerBase"/> is running.
        /// </summary>
        /// <value>
        ///   <c>true</c> if running; otherwise, <c>false</c>.
        /// </value>
        public override bool Running => (WorkerThread != null && WorkerThread.IsAlive) || (MessageProcessing !=null && MessageProcessing.AsyncTaskCount > 0);

        /// <summary>
        /// Tries to force terminate the thread if needed
        /// </summary>
        public override void TryForceTerminate()
        {
            if (!Running) return;

            AttemptToTerminate(); //one last request to terminate without an abort or a spin and wait

            if (WorkerThread == null || !WorkerThread.IsAlive) return;

            StopThread.TryForceTerminate(WorkerThread);
        }
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            if (Interlocked.Increment(ref DisposeCount) != 1) return;

            if (Running)
            {
                Stop();
                TryForceTerminate();
            }
            base.Dispose();
        }
    }
}
