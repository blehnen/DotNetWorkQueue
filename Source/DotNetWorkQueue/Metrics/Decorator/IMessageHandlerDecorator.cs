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
namespace DotNetWorkQueue.Metrics.Decorator
{
    internal class MessageHandlerDecorator : IMessageHandler
    {
        private readonly IMessageHandler _handler;
        private readonly ITimer _runCodeTimer;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueCreationDecorator" /> class.
        /// </summary>
        /// <param name="metrics">The metrics factory.</param>
        /// <param name="handler">The handler.</param>
        /// <param name="connectionInformation">The connection information.</param>
        public MessageHandlerDecorator(IMetrics metrics,
            IMessageHandler handler,
            IConnectionInformation connectionInformation)
        {
            var name = "MessageHandler";
            _runCodeTimer = metrics.Timer($"{connectionInformation.QueueName}.{name}.HandleTimer", Units.Calls);
            _handler = handler;
        }

        /// <summary>
        /// Handles the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="workerNotification">The worker notification.</param>
        public void Handle(IReceivedMessageInternal message, IWorkerNotification workerNotification)
        {
            using (_runCodeTimer.NewContext())
            {
                _handler.Handle(message, workerNotification);
            }
        }
    }
}
