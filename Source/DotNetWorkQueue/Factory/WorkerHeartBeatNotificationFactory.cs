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
using System.Threading;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Factory
{
    /// <summary>
    /// Creates a new instance of <see cref="IWorkerHeartBeatNotification"/>
    /// </summary>
    public class WorkerHeartBeatNotificationFactory : IWorkerHeartBeatNotificationFactory
    {
        private readonly IContainerFactory _container;
        private readonly IHeartBeatConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorkerHeartBeatNotificationFactory" /> class.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="configuration">The configuration.</param>
        public WorkerHeartBeatNotificationFactory(IContainerFactory container, IHeartBeatConfiguration configuration)
        {
            Guard.NotNull(() => container, container);
            Guard.NotNull(() => configuration, configuration);
            _container = container;
            _configuration = configuration;
        }
        /// <summary>
        /// Creates a new instance of <see cref="IWorkerHeartBeatNotification" />
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// new instance of <see cref="IWorkerHeartBeatNotification" />
        /// </returns>
        public IWorkerHeartBeatNotification Create(CancellationToken cancellationToken)
        {
            if (!_configuration.Enabled) return new WorkerHeartBeatNotificationNoOp();
            var notification = _container.Create().GetInstance<IWorkerHeartBeatNotification>();
            notification.ExceptionHasOccured = cancellationToken;
            return notification;
        }
    }
}
