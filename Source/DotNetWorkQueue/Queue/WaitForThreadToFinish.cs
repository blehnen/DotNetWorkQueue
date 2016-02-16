// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using System.Diagnostics;
using System.Threading;
using DotNetWorkQueue.Logging;
namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Waits until a thread has indicated its done processing
    /// </summary>
    public class WaitForThreadToFinish
    {
        private readonly ILog _log;
        /// <summary>
        /// Initializes a new instance of the <see cref="WaitForThreadToFinish"/> class.
        /// </summary>
        /// <param name="log">The log.</param>
        public WaitForThreadToFinish(ILogFactory log)
        {
            Guard.NotNull(() => log, log);
            _log = log.Create();
        }
        /// <summary>
        /// Waits for specified worker thread to finish.
        /// </summary>
        /// <param name="workerThread">The worker thread.</param>
        /// <returns></returns>
        public bool Wait(Thread workerThread)
        {
            return Wait(workerThread, null);
        }
        /// <summary>
        ///  Waits for specified worker thread to finish, or until the timeout period has been reached.
        /// </summary>
        /// <param name="workerThread">The worker thread.</param>
        /// <param name="timeout">The timeout.</param>
        /// <returns></returns>
        public bool Wait(Thread workerThread, TimeSpan? timeout)
        {
            var iLogCount = 0;
            Stopwatch timer = null;
            if (timeout.HasValue)
            {
                timer = new Stopwatch();
                timer.Start();
            }
            while (workerThread != null && workerThread.IsAlive)
            {
                if (iLogCount == 0 || iLogCount % 5000 == 0)
                {
                    _log?.Warn($"Still waiting for thread {workerThread.Name} to stop or cancel");
                }
                Thread.Sleep(20);
                iLogCount = iLogCount + 20;

                if(timer != null && timer.Elapsed >= timeout.Value)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
