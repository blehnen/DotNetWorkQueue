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

using DotNetWorkQueue.Queue;

namespace DotNetWorkQueue.Metrics.Decorator
{
    internal class QueueCreationDecorator : IQueueCreation
    {
        private readonly IQueueCreation _handler;
        private readonly ITimer _createQueueTimer;
        private readonly ITimer _removeQueueTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueCreationDecorator" /> class.
        /// </summary>
        /// <param name="metrics">The metrics factory.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public QueueCreationDecorator(IMetrics metrics,
            IQueueCreation handler,
            IConnectionInformation connectionInformation)
        {
            var name = "QueueCreation";
            _createQueueTimer = metrics.Timer($"{connectionInformation.QueueName}.{name}.CreateQueueTimer", Units.Calls);
            _removeQueueTimer = metrics.Timer($"{connectionInformation.QueueName}.{name}.RemoveQueueTimer", Units.Calls);
            _handler = handler;
        }

        /// <inheritdoc />
        public IConnectionInformation ConnectionInfo => _handler.ConnectionInfo;

        /// <summary>
        /// Gets the base transport options.
        /// </summary>
        public IBaseTransportOptions BaseTransportOptions => _handler.BaseTransportOptions;

        /// <inheritdoc />
        public QueueScript CreationScript => _handler.CreationScript;

        /// <inheritdoc />
        public bool QueueExists => _handler.QueueExists;

        /// <inheritdoc />
        public ICreationScope Scope => _handler.Scope;

        /// <inheritdoc />
        public QueueCreationResult CreateQueue()
        {
            using (_createQueueTimer.NewContext())
            {
                return _handler.CreateQueue();
            }
        }

        /// <inheritdoc />
        public QueueRemoveResult RemoveQueue()
        {
            using (_removeQueueTimer.NewContext())
            {
                return _handler.RemoveQueue();
            }
        }

        /// <inheritdoc />
        public bool IsDisposed => _handler.IsDisposed;

        /// <inheritdoc />
        public void Dispose()
        {
            _handler.Dispose();
        }
    }
}
