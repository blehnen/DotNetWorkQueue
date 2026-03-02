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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Messages;

namespace DotNetWorkQueue.Dashboard.Api.Integration.Tests.Helpers
{
    public class TransportFixture<TTransportInit, TQueueCreation> : IDisposable
        where TTransportInit : ITransportInit, new()
        where TQueueCreation : class, IQueueCreation
    {
        private readonly QueueCreationContainer<TTransportInit> _creationContainer;
        private readonly TQueueCreation _creation;
        private ICreationScope _scope;

        public string QueueName { get; }
        public string ConnectionString { get; }
        public QueueConnection QueueConnection { get; }

        public TransportFixture(string queueName, string connectionString, Action<TQueueCreation> setOptions = null)
        {
            QueueName = queueName;
            ConnectionString = connectionString;
            QueueConnection = new QueueConnection(queueName, connectionString);

            _creationContainer = new QueueCreationContainer<TTransportInit>();
            _creation = _creationContainer.GetQueueCreation<TQueueCreation>(QueueConnection);

            setOptions?.Invoke(_creation);

            var result = _creation.CreateQueue();
            if (!result.Success)
                throw new InvalidOperationException($"Failed to create queue: {result.ErrorMessage}");

            _scope = _creation.Scope;
        }

        public ICreationScope Scope => _scope;

        public void SendMessages<TMessage>(int count, Func<IAdditionalMessageData> generateData = null)
            where TMessage : class, new()
        {
            using (var container = new QueueContainer<TTransportInit>(
                       serviceRegister => serviceRegister.RegisterNonScopedSingleton(_scope)))
            {
                using (var producer = container.CreateProducer<TMessage>(QueueConnection))
                {
                    for (var i = 0; i < count; i++)
                    {
                        IQueueOutputMessage result;
                        if (generateData != null)
                        {
                            result = producer.Send(new TMessage(), generateData());
                        }
                        else
                        {
                            result = producer.Send(new TMessage());
                        }

                        if (result.HasError)
                            throw new InvalidOperationException(
                                $"Failed to send message {i}: {result.SendingException?.Message}",
                                result.SendingException);
                    }
                }
            }
        }

        public void Dispose()
        {
            try
            {
                _creation?.RemoveQueue();
            }
            catch
            {
                // best-effort cleanup
            }

            _creation?.Dispose();
            _creationContainer?.Dispose();
            _scope?.Dispose();
        }
    }
}
