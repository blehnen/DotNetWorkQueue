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
using System.Threading;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Stops a thread by aborting it if configured to do; otherwise it will wait (forever if needed) until the thread dies.
    /// </summary>
    public class StopThread
    {
        private readonly IAbortWorkerThread _abortWorkerThread;
        private readonly WaitForThreadToFinish _waitForThreadToFinish;

        /// <summary>
        /// Initializes a new instance of the <see cref="StopThread"/> class.
        /// </summary>
        /// <param name="abortWorkerThread">The abort worker thread.</param>
        /// <param name="waitForThreadToFinish">The wait for thread to finish.</param>
        public StopThread(IAbortWorkerThread abortWorkerThread,
            WaitForThreadToFinish waitForThreadToFinish)
        {
            Guard.NotNull(() => abortWorkerThread, abortWorkerThread);
            Guard.NotNull(() => waitForThreadToFinish, waitForThreadToFinish);

            _abortWorkerThread = abortWorkerThread;
            _waitForThreadToFinish = waitForThreadToFinish; 
        }

        /// <summary>
        /// Stops a thread by aborting it if configured to do; otherwise it will wait (forever if needed) until the thread dies.
        /// </summary>
        /// <param name="workerThread">The worker thread.</param>
        /// <returns></returns>
        public bool TryForceTerminate(Thread workerThread)
        {
            if (_abortWorkerThread.Abort(workerThread)) return true;

            //wait for the thread to exit
            _waitForThreadToFinish.Wait(workerThread);

            return true;
        }
    }
}
