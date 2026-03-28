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
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Waits until a thread has indicated its done processing
    /// </summary>
    public class WaitForThreadToFinish
    {
        private readonly ILogger _log;
        /// <summary>
        /// Initializes a new instance of the <see cref="WaitForThreadToFinish"/> class.
        /// </summary>
        /// <param name="log">The log.</param>
        public WaitForThreadToFinish(ILogger log)
        {
            Guard.NotNull(() => log, log);
            _log = log;
        }
        /// <summary>
        ///  Waits for specified worker task to finish, or until the timeout period has been reached.
        /// </summary>
        /// <param name="workerTask">The worker task.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        public bool Wait(Task workerTask, TimeSpan? timeout = null)
        {
            if (workerTask == null || workerTask.IsCompleted)
                return true;

            try
            {
                if (timeout.HasValue)
                    return workerTask.Wait(timeout.Value);

                workerTask.Wait();
                return true;
            }
            catch (AggregateException)
            {
                return true; // Task faulted or canceled -- it has finished
            }
        }
    }
}
