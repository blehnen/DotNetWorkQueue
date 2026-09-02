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
using System.Diagnostics;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Factory
{
    /// <summary>
    /// Creates new instances of <see cref="IWorkerNotification"/>
    /// </summary>
    /// <remarks>
    /// One of these is created for every message consumed. Resolving it from the container cost
    /// 437 ns against 20 ns to construct the same object, so when the registration is the default
    /// one this builds it directly. A replaced registration still goes through the container.
    /// </remarks>
    internal class WorkerNotificationFactory : IWorkerNotificationFactory
    {
        private readonly IContainerFactory _container;
        private readonly IHeaders _headerNames;
        private readonly IQueueCancelWork _cancelWork;
        private readonly TransportConfigurationReceive _configuration;
        private readonly ILogger _log;
        private readonly IMetrics _metrics;
        private readonly ActivitySource _tracer;

        /// <summary>
        /// Whether the container still produces <see cref="WorkerNotification"/>. Deferred because
        /// asking the container locks it, and this factory is built while registration may still
        /// be in progress.
        /// </summary>
        private readonly Lazy<bool> _isDefaultRegistration;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkerNotificationFactory"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="headerNames">The header names.</param>
        /// <param name="cancelWork">The cancel work.</param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="log">The log.</param>
        /// <param name="metrics">The metrics factory.</param>
        /// <param name="tracer">The tracer.</param>
        public WorkerNotificationFactory(IContainerFactory container,
            IHeaders headerNames,
            IQueueCancelWork cancelWork,
            TransportConfigurationReceive configuration,
            ILogger log,
            IMetrics metrics,
            ActivitySource tracer)
        {
            Guard.NotNull(container);
            _container = container;
            _headerNames = headerNames;
            _cancelWork = cancelWork;
            _configuration = configuration;
            _log = log;
            _metrics = metrics;
            _tracer = tracer;

            _isDefaultRegistration = new Lazy<bool>(() =>
                _container.Create().GetImplementationType<IWorkerNotification>() == typeof(WorkerNotification));
        }
        /// <summary>
        /// Creates a new instance of <see cref="IWorkerNotification" />
        /// </summary>
        /// <returns></returns>
        public IWorkerNotification Create()
        {
            if (!_isDefaultRegistration.Value)
                return _container.Create().GetInstance<IWorkerNotification>();

            return new WorkerNotification(_headerNames, _cancelWork, _configuration, _log, _metrics, _tracer);
        }
    }
}
