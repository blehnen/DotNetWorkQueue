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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Queue
{
    /// <inheritdoc />
    public class WorkerCollection : IWorkerCollection
    {
        private readonly IWorkerConfiguration _workerConfiguration;
        private List<IWorker> _workers;
        private readonly IWorkerFactory _workerFactory;
        private readonly ILog _log;
        private readonly StopWorker _stopWorker;
        private readonly IWorkerWaitForEventOrCancel _workerPause;

        /// <summary>
        /// Event that will be raised each time message delivery fails.
        /// </summary>
        public event EventHandler<WorkerErrorEventArgs> UserException;

        /// <summary>
        /// Event that will be raised if an exception occurs outside of user code.
        /// </summary>
        public event EventHandler<WorkerErrorEventArgs> SystemException;

        private readonly object _workerLock = new object();

        private int _disposeCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkerCollection"/> class.
        /// </summary>
        /// <param name="workerConfiguration">The worker configuration.</param>
        /// <param name="workerFactory">The worker factory.</param>
        /// <param name="stopWorker">The stop worker.</param>
        /// <param name="log">The log.</param>
        /// <param name="workerPause">The worker pause.</param>
        public WorkerCollection(IWorkerConfiguration workerConfiguration,
            IWorkerFactory workerFactory,
            StopWorker stopWorker,
            ILogFactory log,
            IWorkerWaitForEventOrCancel workerPause)
        {
            Guard.NotNull(() => workerConfiguration, workerConfiguration);
            Guard.NotNull(() => workerFactory, workerFactory);
            Guard.NotNull(() => stopWorker, stopWorker);
            Guard.NotNull(() => log, log);
            Guard.NotNull(() => workerPause, workerPause);

            _workerConfiguration = workerConfiguration;
            _workerFactory = workerFactory;
            _stopWorker = stopWorker;
            _log = log.Create();
            _workerPause = workerPause;
        }

        /// <inheritdoc />
        public void Start()
        {
            lock (_workerLock)
            {
                if (_workers != null && _workers.Count > 0)
                {
                    throw new DotNetWorkQueueException("Start must only be called once");
                }
            }

            _log.InfoFormat("Initializing with {0} workers", _workerConfiguration.WorkerCount);
            CreateWorkers();

            lock (_workerLock)
            {
                if (_workers == null || _workers.Count <= 0) return;
            }

            lock (_workerLock)
            {
                _workers.AsParallel().ForAll(w => w.Start());
            }
        }

        /// <inheritdoc />
        public void Stop()
        {
            //stop all workers
            lock (_workerLock)
            {
                if (_workers == null || _workers.Count == 0)
                    return;

                _workers.AsParallel().ForAll(w => w.Stop());
                _stopWorker.Stop(_workers);
                _workers.Clear();
            }
        }

        /// <inheritdoc />
        public void PauseWorkers()
        {
            _workerPause.Reset();
        }

        /// <inheritdoc />
        public void ResumeWorkers()
        {
            _workerPause.Set();
        }

        /// <inheritdoc />
        public bool AllWorkersAreIdle
        {
            get
            {
                if (IsDisposed)
                    return false;

                lock (_workerLock)
                {
                    return _workers != null && _workers.TrueForAll(worker => worker.IdleStatus == WorkerIdleStatus.Idle);
                }
            }
        }
        /// <inheritdoc />
        public IWorkerConfiguration Configuration => _workerConfiguration;

        /// <inheritdoc />
        public bool IsDisposed => Interlocked.CompareExchange(ref _disposeCount, 0, 0) != 0;

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;
            if (Interlocked.Increment(ref _disposeCount) == 1)
            {
                Stop();
            }
        }

        /// <summary>
        /// Creates the workers
        /// </summary>
        private void CreateWorkers()
        {
            Guard.IsValid(() => _workerConfiguration.WorkerCount, _workerConfiguration.WorkerCount, i => i > 0,
                "numberOfWorkers must be greater than 0");

            //this collection contains all workers except the primary worker
            var workerCount = _workerConfiguration.WorkerCount - 1;
            lock (_workerLock)
            {
                if (workerCount > 0)
                {
                    _workers = new List<IWorker>(workerCount);
                    while (_workers.Count < workerCount)
                    {
                        AddWorker();
                    }
                }
                else
                {
                    _workers = new List<IWorker>(0);
                }
            }
        }

        /// <summary>
        /// Adds a new worker.
        /// </summary>
        private void AddWorker()
        {
            var worker = _workerFactory.Create();
            _workers.Add(worker);
            worker.SystemException += RaiseSystemException;
            worker.UserException += RaiseUserException;
        }

        /// <summary>
        /// Raises the system exception.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="WorkerErrorEventArgs"/> instance containing the event data.</param>
        private void RaiseSystemException(object sender, WorkerErrorEventArgs e)
        {
            SystemException?.Invoke(sender, e);
        }

        /// <summary>
        /// Raises the user exception.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="WorkerErrorEventArgs"/> instance containing the event data.</param>
        private void RaiseUserException(object sender, WorkerErrorEventArgs e)
        {
            UserException?.Invoke(sender, e);
        }
    }
}
