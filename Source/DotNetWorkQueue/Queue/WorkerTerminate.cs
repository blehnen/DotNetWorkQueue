// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
using System.Threading;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Attempts to terminate via joining to it.
    /// </summary>
    public class WorkerTerminate
    {
        /// <summary>
        /// Attempts to terminate the passed in thread
        /// </summary>
        /// <param name="workerThread">The worker thread.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        public bool AttemptToTerminate(Thread workerThread, TimeSpan? timeout)
        {
            if (workerThread == null || !workerThread.IsAlive)
                return true; //if the thread is null or not alive, its terminated

            if (timeout.HasValue)
            {
                return workerThread.Join(timeout.Value);
            }
            workerThread.Join();
            return true;
        }
    }
}
