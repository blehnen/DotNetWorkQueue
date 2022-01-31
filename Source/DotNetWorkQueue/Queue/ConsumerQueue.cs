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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Defines a queue that can process messages
    /// </summary>
    public class ConsumerQueue : BaseQueue, IConsumerQueue
    {
        private readonly QueueConsumerConfiguration _configuration;
        private readonly Lazy<IPrimaryWorker> _primaryWorker;
        private readonly IRegisterMessages _registerMessages;
        private readonly IQueueMonitor _queueMonitor;
        private readonly StopWorker _stopWorker;

        private int _disposeCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsumerQueue" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="queueMonitor">The queue monitor.</param>
        /// <param name="log">The log.</param>
        /// <param name="registerMessages">The register messages.</param>
        /// <param name="primaryWorkerFactory">The primary worker factory.</param>
        /// <param name="stopWorker">The stop worker.</param>
        public ConsumerQueue(
            QueueConsumerConfiguration configuration,
            IQueueMonitor queueMonitor,
            ILogger log,
            IRegisterMessages registerMessages,
            IPrimaryWorkerFactory primaryWorkerFactory,
            StopWorker stopWorker)
            : base(log)
        {
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => queueMonitor, queueMonitor);
            Guard.NotNull(() => log, log);
            Guard.NotNull(() => registerMessages, registerMessages);
            Guard.NotNull(() => primaryWorkerFactory, primaryWorkerFactory);
            Guard.NotNull(() => stopWorker, stopWorker);

            _configuration = configuration;
            _queueMonitor = queueMonitor;
            _registerMessages = registerMessages;
            _stopWorker = stopWorker;
            _primaryWorker = new Lazy<IPrimaryWorker>(() =>
            {
                var worker = primaryWorkerFactory.Create();
                worker.UserException += LogUserException;
                worker.SystemException += LogSystemException;
                return worker;
            });
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>
        /// The configuration.
        /// </value>
        public QueueConsumerConfiguration Configuration
        {
            get
            {
                ThrowIfDisposed();
                return _configuration;
            }
        }

        /// <summary>
        /// Starts the queue.
        /// </summary>
        /// <typeparam name="T">The type of the message</typeparam>
        /// <param name="workerAction">The action the worker should call when a message is received.</param>
        /// <exception cref="DotNetWorkQueueException">Start must only be called 1 time</exception>
        public void Start<T>(Action<IReceivedMessage<T>, IWorkerNotification> workerAction)
            where T : class
        {
            ThrowIfDisposed();

            if (Started)
            {
                throw new DotNetWorkQueueException("Start must only be called 1 time");
            }

            Guard.NotNull(() => workerAction, workerAction);
            Started = true;

            _registerMessages.Register(workerAction);
            _queueMonitor.Start();
            ShouldWork = true;
            _primaryWorker.Value.Start();
            _configuration.SetReadOnly();
            Log.LogInformation("Queue started");

        }

        #region Dispose
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;

            if (Interlocked.Increment(ref _disposeCount) != 1) return;

            ShouldWork = false;
            _queueMonitor.Stop();

            if (_primaryWorker.IsValueCreated)
            {
                //stop looking for pending work
                _stopWorker.SetCancelTokenForStopping();

                _stopWorker.StopPrimary(_primaryWorker.Value);

                _primaryWorker.Value.UserException -= LogUserException;
                _primaryWorker.Value.SystemException -= LogSystemException;
                _primaryWorker.Value.Dispose();
            }
            base.Dispose(true);
        }
        #endregion
    }
}
