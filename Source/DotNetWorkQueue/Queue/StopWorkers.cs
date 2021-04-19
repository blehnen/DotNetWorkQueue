// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Gracefully shuts down a <see cref="IWorker"/> instance(s)
    /// </summary>
    public class StopWorker
    {
        private readonly IQueueCancelWork _cancelWorkSource;
        private readonly IWorkerConfiguration _configuration;
        private readonly ILogger _log;
        private readonly IWorkerWaitForEventOrCancel _waitForEventOrCancel;

        /// <summary>
        /// Initializes a new instance of the <see cref="StopWorker"/> class.
        /// </summary>
        /// <param name="cancelWorkSource">The cancel work source.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="log">The log.</param>
        /// <param name="waitForEventOrCancel">The wait for event or cancel.</param>
        public StopWorker(IQueueCancelWork cancelWorkSource,
            IWorkerConfiguration configuration,
            ILogger log,
            IWorkerWaitForEventOrCancel waitForEventOrCancel)
        {
            Guard.NotNull(() => cancelWorkSource, cancelWorkSource);
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => log, log);
            Guard.NotNull(() => waitForEventOrCancel, waitForEventOrCancel);

            _cancelWorkSource = cancelWorkSource;
            _configuration = configuration;
            _log = log;
            _waitForEventOrCancel = waitForEventOrCancel;
        }

        /// <summary>
        /// Sets the cancel flag indicating that workers should no longer look for new work to process
        /// </summary>
        public void SetCancelTokenForStopping()
        {
            _cancelWorkSource.StopTokenSource.Cancel();
        }

        /// <summary>
        /// Stops the specified workers.
        /// </summary>
        /// <param name="workers">The workers.</param>
        public void Stop(List<IWorker> workers)
        {
            Guard.NotNull(() => workers, workers);

            _waitForEventOrCancel.Set();
            _waitForEventOrCancel.Cancel();

            if (workers.Count == 0) return;

            //wait for workers to stop
            WaitForDelegate.Wait(() => workers.Any(r => r.Running), _configuration.TimeToWaitForWorkersToStop);
            if (workers.Any(r => r.Running))
            {
                workers.AsParallel().ForAll(w => w.AttemptToTerminate());
            }

            var alCancel = workers.Where(worker => worker.Running).ToList();

            //attempt to cancel workers if any are still running
            if (alCancel.Count > 0)
            {
                _cancelWorkSource.CancellationTokenSource.Cancel();
                WaitForDelegate.Wait(() => alCancel.Any(r => r.Running), _configuration.TimeToWaitForWorkersToCancel);
                if (alCancel.Any(r => r.Running))
                {
                    alCancel.AsParallel().ForAll(w => w.AttemptToTerminate());
                }
            }

            //force kill workers that are still running by aborting the thread, or waiting until work has completed
            var alForceTerminate = workers.Where(worker => worker.Running).ToList();
            if (alForceTerminate.Count > 0)
            {
                alForceTerminate.AsParallel().ForAll(w => w.TryForceTerminate());
            }

            //dispose workers - they are created by a factory, and must be disposed of
            foreach (var worker in workers)
            {
                try
                {
                    worker.Dispose();
                }
                catch (Exception e)
                {
                    _log.LogError("An error has occurred while disposing of a worker", e);
                }
            }
        }

        /// <summary>
        /// Stops the specified primary worker
        /// </summary>
        /// <param name="worker">The worker.</param>
        public void StopPrimary(IPrimaryWorker worker)
        {
            Guard.NotNull(() => worker, worker);

            _waitForEventOrCancel.Set();
            _waitForEventOrCancel.Cancel();

            //stop is a blocking operation for the primary worker
            Task.Run(worker.Stop);

            //wait for workers to stop
            WaitForDelegate.Wait(() => worker.Running, _configuration.TimeToWaitForWorkersToStop);
            if (worker.Running)
            {
                worker.AttemptToTerminate();
            }

            //attempt to cancel workers if any are still running
            if (worker.Running)
            {
                _cancelWorkSource.CancellationTokenSource.Cancel();
                WaitForDelegate.Wait(() => worker.Running, _configuration.TimeToWaitForWorkersToCancel);
                if (worker.Running)
                {
                    worker.AttemptToTerminate();
                }
            }

            //force kill workers that are still running by aborting the thread, or waiting until work has completed
            if (worker.Running)
            {
                worker.TryForceTerminate();
            }
        }
    } 
}
