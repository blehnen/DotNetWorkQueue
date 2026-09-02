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
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Validation;
namespace DotNetWorkQueue.Factory
{
    /// <summary>
    /// Creates new instances of <see cref="IMessageContext"/>
    /// </summary>
    /// <remarks>
    /// One of these is created for every message consumed, so it carries the same direct-build
    /// shortcut as <see cref="WorkerNotificationFactory"/>, for the same measured reason: the
    /// resolve is what costs, not the object.
    /// </remarks>
    internal class MessageContextFactory : IMessageContextFactory
    {
        private readonly IContainerFactory _container;
        private readonly IWorkerNotificationFactory _workerNotificationFactory;

        /// <summary>
        /// Whether the container still produces <see cref="MessageContext"/>. Deferred because
        /// asking the container locks it, and this factory is built while registration may still
        /// be in progress.
        /// </summary>
        private readonly Lazy<bool> _isDefaultRegistration;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageContextFactory"/> class.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="workerNotificationFactory">The worker notification factory.</param>
        public MessageContextFactory(IContainerFactory container,
            IWorkerNotificationFactory workerNotificationFactory)
        {
            Guard.NotNull(container);
            Guard.NotNull(workerNotificationFactory);
            _container = container;
            _workerNotificationFactory = workerNotificationFactory;

            _isDefaultRegistration = new Lazy<bool>(() =>
                _container.Create().GetImplementationType<IMessageContext>() == typeof(MessageContext));
        }
        /// <summary>
        /// Creates a new instance of <see cref="IMessageContext" />
        /// </summary>
        /// <returns></returns>
        public IMessageContext Create()
        {
            if (!_isDefaultRegistration.Value)
                return _container.Create().GetInstance<IMessageContext>();

            return new MessageContext(_workerNotificationFactory);
        }
    }
}
