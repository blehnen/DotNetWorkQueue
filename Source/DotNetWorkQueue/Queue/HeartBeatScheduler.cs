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
using System.Linq.Expressions;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Transport.Memory.Basic;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Queue
{
    /// <inheritdoc />
    public class HeartBeatScheduler : IHeartBeatScheduler
    {
        private readonly IHeartBeatThreadPoolConfiguration _configuration;
        private readonly IGetTimeFactory _timeFactory;
        private JobSchedulerContainer _container;
        private IJobScheduler _scheduler;
        private readonly ILogger _log;
        private SchedulerContainer _consumerContainer;
        private ATaskScheduler _consumerScheduler;
        private ITaskFactory _taskFactory;
        private QueueContainer<MemoryMessageQueueInit> _queueContainer;
        private IConsumerMethodQueueScheduler _consumer;

        private const string QueueName = "HeartBeatWorkers";
        private const string Connection = "Memory";

        private readonly object _startup = new object();

        /// <summary>
        /// Initializes a new instance of the <see cref="HeartBeatScheduler"/> class.
        /// </summary>
        public HeartBeatScheduler(IHeartBeatThreadPoolConfiguration configuration,
            IGetTimeFactory timeFactory, ILogger log)
        {
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => timeFactory, timeFactory);
            Guard.NotNull(() => log, log);

            _configuration = configuration;
            _timeFactory = timeFactory;
            _log = log;
        }

        /// <inheritdoc />
        public bool IsShuttingDown => _scheduler != null && _scheduler.IsShuttingDown;

        /// <inheritdoc />
        public bool IsDisposed => _scheduler != null && _scheduler.IsDisposed;

        /// <inheritdoc />
        public IScheduledJob AddUpdateJob(string jobName, string schedule,
            Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> job)
        {
            if (IsDisposed) return null;

            if (_consumer == null)
                CreateScheduler();

            return _scheduler.AddUpdateJob<MemoryMessageQueueInit, JobQueueCreation>(jobName, new QueueConnection(QueueName, Connection), schedule, job, null, null, true, default, true);
        }

        /// <inheritdoc />
        public bool RemoveJob(string name)
        {
            return _scheduler.RemoveJob(name);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _consumer?.Dispose();
            _queueContainer?.Dispose();
            _consumerScheduler?.Dispose();
            _consumerContainer?.Dispose();

            _scheduler?.Dispose();
            _container?.Dispose();
        }

        private void CreateScheduler()
        {
            lock (_startup)
            {
                if (_consumer != null) return;
                _container = new JobSchedulerContainer(container =>
                    container.Register(() => _timeFactory, LifeStyles.Singleton)
                        .Register(() => _log, LifeStyles.Singleton));
                _scheduler = _container.CreateJobScheduler();
                _scheduler.Start();

                _consumerContainer = new SchedulerContainer(container => container.Register(() => _log, LifeStyles.Singleton));
                _consumerScheduler = _consumerContainer.CreateTaskScheduler();
                _taskFactory = _consumerContainer.CreateTaskFactory(_consumerScheduler);

                _taskFactory = _consumerContainer.CreateTaskFactory(_consumerScheduler);
                _taskFactory.Scheduler.Configuration.MaximumThreads = _configuration.ThreadsMax;
                _taskFactory.Scheduler.Configuration.WaitForThreadPoolToFinish =
                    _configuration.WaitForThreadPoolToFinish;
                _taskFactory.Scheduler.Start();
                _queueContainer = new QueueContainer<MemoryMessageQueueInit>(container => container.Register(() => _log, LifeStyles.Singleton));
                _consumer = _queueContainer.CreateConsumerMethodQueueScheduler( new QueueConnection(QueueName, Connection),
                    _taskFactory);
                _consumer.Start();
            }
        }
    }
}
