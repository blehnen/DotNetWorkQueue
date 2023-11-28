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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Defines a queue that can process messages in an async fashion.
    /// </summary>
    public class ConsumerQueueAsync : BaseQueue, IConsumerQueueAsync
    {
        private readonly QueueConsumerConfiguration _configuration;
        private readonly Lazy<IPrimaryWorker> _primaryWorker;
        private readonly StopWorker _stopWorker;
        private readonly IQueueMonitor _queueMonitor;
        private readonly IRegisterMessagesAsync _registerMessagesAsync;

        private int _disposeCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsumerQueueAsync" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="workerFactory">The worker factory.</param>
        /// <param name="log">The log.</param>
        /// <param name="registerMessagesAsync">The register messages asynchronous.</param>
        /// <param name="stopWorker">The stop worker.</param>
        /// <param name="queueMonitor">The queue monitor.</param>
        /// <param name="consumerQueueErrorNotification">notifications for consumer queue errors</param>
        /// <param name="consumerQueueNotification">notifications for consumer queue messages</param>
        public ConsumerQueueAsync(
            QueueConsumerConfiguration configuration,
            IPrimaryWorkerFactory workerFactory,
            ILogger log,
            IRegisterMessagesAsync registerMessagesAsync,
            StopWorker stopWorker,
            IQueueMonitor queueMonitor,
            IConsumerQueueNotification consumerQueueNotification,
            IConsumerQueueErrorNotification consumerQueueErrorNotification)
            : base(log, consumerQueueNotification, consumerQueueErrorNotification)
        {
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => workerFactory, workerFactory);
            Guard.NotNull(() => log, log);
            Guard.NotNull(() => registerMessagesAsync, registerMessagesAsync);
            Guard.NotNull(() => stopWorker, stopWorker);
            Guard.NotNull(() => queueMonitor, queueMonitor);

            _configuration = configuration;
            _primaryWorker = new Lazy<IPrimaryWorker>(() =>
            {
                var worker = workerFactory.Create();
                return worker;
            });

            _stopWorker = stopWorker;
            _queueMonitor = queueMonitor;
            _registerMessagesAsync = registerMessagesAsync;
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
        /// <param name="workerAction">The worker action.</param>
        /// <param name="notifications">Allows subscribing to notification of message queue events, such as completed or error</param>
        /// <exception cref="System.ObjectDisposedException">Start</exception>
        /// <exception cref="DotNetWorkQueueException">Start must only be called 1 time</exception>
        public void Start<T>(Func<IReceivedMessage<T>, IWorkerNotification, Task> workerAction, ConsumerQueueNotifications notifications = null)
            where T : class
        {
            ThrowIfDisposed();

            Guard.NotNull(() => workerAction, workerAction);

            if (Started)
            {
                throw new DotNetWorkQueueException("Start must only be called 1 time");
            }

            Started = true;

            ShouldWork = true;
            _registerMessagesAsync.Register(workerAction);
            _queueMonitor.Start();
            _primaryWorker.Value.Start();
            _configuration.SetReadOnly();
            base.SetupNotifications(notifications);
            Log.LogInformation("Queue started");
        }
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (!disposing) return;

            if (Interlocked.Increment(ref _disposeCount) != 1) return;

            //stop monitor process(es)
            _queueMonitor.Stop();

            //tell the worker to stop
            if (_primaryWorker.IsValueCreated)
            {
                _stopWorker.SetCancelTokenForStopping();
                _stopWorker.StopPrimary(_primaryWorker.Value);
                _primaryWorker.Value.Dispose();
            }
            base.Dispose(true);
        }
    }
}
