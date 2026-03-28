// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using System.Threading.Tasks;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Waits for a worker thread to finish its current work before returning.
    /// </summary>
    public class StopThread
    {
        private readonly WaitForThreadToFinish _waitForThreadToFinish;

        /// <summary>
        /// Initializes a new instance of the <see cref="StopThread"/> class.
        /// </summary>
        /// <param name="waitForThreadToFinish">The wait for thread to finish.</param>
        public StopThread(WaitForThreadToFinish waitForThreadToFinish)
        {
            Guard.NotNull(() => waitForThreadToFinish, waitForThreadToFinish);
            _waitForThreadToFinish = waitForThreadToFinish;
        }

        /// <summary>
        /// Waits for the worker task to finish its current work before returning.
        /// </summary>
        /// <param name="workerTask">The worker task.</param>
        /// <returns>Always returns true after waiting for the task to finish.</returns>
        public bool TryForceTerminate(Task workerTask)
        {
            _waitForThreadToFinish.Wait(workerTask);
            return true;
        }
    }
}
