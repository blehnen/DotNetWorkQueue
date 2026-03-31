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
using System;
using System.Threading.Tasks;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Attempts to terminate via joining to it.
    /// </summary>
    public class WorkerTerminate
    {
        /// <summary>
        /// Attempts to terminate the passed in task
        /// </summary>
        /// <param name="workerTask">The worker task.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        public bool AttemptToTerminate(Task workerTask, TimeSpan? timeout)
        {
            if (workerTask == null || workerTask.IsCompleted)
                return true; //if the task is null or completed, its terminated

            if (timeout.HasValue)
            {
                return workerTask.Wait(timeout.Value);
            }
            workerTask.Wait();
            return true;
        }
    }
}
