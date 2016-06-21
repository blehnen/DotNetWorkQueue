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
namespace DotNetWorkQueue.Metrics.Decorator
{
    internal class QueueCreationDecorator: IQueueCreation
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
            var name = handler.GetType().Name;
            _createQueueTimer = metrics.Timer($"{connectionInformation.QueueName}.{name}.CreateQueueTimer", Units.Calls);
            _removeQueueTimer = metrics.Timer($"{connectionInformation.QueueName}.{name}.RemoveQueueTimer", Units.Calls);
            _handler = handler;
        }

        /// <summary>
        /// Gets or sets the connection information.
        /// </summary>
        /// <value>
        /// The connection information.
        /// </value>
        public IConnectionInformation ConnectionInfo => _handler.ConnectionInfo;

        /// <summary>
        /// Gets or sets a value indicating whether [queue exists].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [queue exists]; otherwise, <c>false</c>.
        /// </value>
        public bool QueueExists => _handler.QueueExists;

        /// <summary>
        /// Gets a disposable creation scope
        /// </summary>
        /// <value>
        /// The scope.
        /// </value>
        /// <remarks>This is used to prevent queues from going out of scope before you have finished working with them. Generally
        /// speaking this only matters for queues that live in-memory. However, a valid object is always returned.</remarks>
        public ICreationScope Scope => _handler.Scope;

        /// <summary>
        /// Tries to create the queue.
        /// </summary>
        /// <returns></returns>
        public QueueCreationResult CreateQueue()
        {
            using (_createQueueTimer.NewContext())
            {
                return _handler.CreateQueue();
            }
        }

        /// <summary>
        /// Tries to the remove queue.
        /// </summary>
        /// <returns></returns>
        public QueueRemoveResult RemoveQueue()
        {
            using (_removeQueueTimer.NewContext())
            {
                return _handler.RemoveQueue();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
        public bool IsDisposed => _handler.IsDisposed;

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public void Dispose()
        {
            _handler.Dispose();
        }
    }
}
