// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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

namespace DotNetWorkQueue
{
    /// <summary>
    /// The root container for consumers and producers
    /// </summary>
    /// <remarks>This interface exists to allow queues to create sub queues. The implementation <seealso cref="QueueContainer{TTransportInit}"/> contains additional methods/properties</remarks>
    /// <seealso cref="System.IDisposable" />
    public interface IQueueContainer: IDisposable
    {
        /// <summary>
        /// Creates the consumer queue.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <returns></returns>
        IConsumerQueue CreateConsumer(QueueConnection queueConnection);

        /// <summary>
        /// Creates the method consumer queue.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <returns></returns>
        IConsumerMethodQueue CreateMethodConsumer(QueueConnection queueConnection);

        /// <summary>
        /// Creates an async consumer queue
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <returns></returns>
        IConsumerQueueAsync CreateConsumerAsync(QueueConnection queueConnection);

        /// <summary>
        /// Creates an async consumer queue that uses a task scheduler. The default task factory will be used.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <returns></returns>
        IConsumerQueueScheduler CreateConsumerQueueScheduler(QueueConnection queueConnection);

        /// <summary>
        /// Creates an async consumer queue that uses a task scheduler. The default task factory will be used.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <returns></returns>
        IConsumerMethodQueueScheduler CreateConsumerMethodQueueScheduler(QueueConnection queueConnection);

        /// <summary>
        /// Creates an async consumer queue that uses a task scheduler
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="factory">The task factory.</param>
        /// <returns></returns>
        IConsumerQueueScheduler CreateConsumerQueueScheduler(QueueConnection queueConnection, ITaskFactory factory);

        /// <summary>
        /// Creates an async consumer queue that uses a task scheduler
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="factory">The task factory.</param>
        /// <returns></returns>
        IConsumerMethodQueueScheduler CreateConsumerMethodQueueScheduler(QueueConnection queueConnection, ITaskFactory factory);

        /// <summary>
        /// Creates an async consumer queue that uses a task scheduler
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="factory">The task factory.</param>
        /// <param name="workGroup">The work group.</param>
        /// <returns></returns>
        IConsumerQueueScheduler CreateConsumerQueueScheduler(QueueConnection queueConnection, ITaskFactory factory, IWorkGroup workGroup);

        /// <summary>
        /// Creates an async consumer queue that uses a task scheduler
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="factory">The task factory.</param>
        /// <param name="workGroup">The work group.</param>
        /// <returns></returns>
        IConsumerMethodQueueScheduler CreateConsumerMethodQueueScheduler(QueueConnection queueConnection,
            ITaskFactory factory, IWorkGroup workGroup);

        /// <summary>
        /// Creates a producer queue.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message.</typeparam>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <returns></returns>
        IProducerQueue<TMessage> CreateProducer<TMessage>(
            QueueConnection queueConnection)
            where TMessage : class;

        /// <summary>
        /// Creates a producer queue for executing linq expressions.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <returns></returns>
        IProducerMethodQueue CreateMethodProducer(
            QueueConnection queueConnection);

        /// <summary>
        /// Creates a producer queue for executing linq expressions
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <returns></returns>
        IProducerMethodJobQueue CreateMethodJobProducer(
            QueueConnection queueConnection);

        /// <summary>
        /// Creates the job scheduler last known event.
        /// </summary>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <returns></returns>
        IJobSchedulerLastKnownEvent CreateJobSchedulerLastKnownEvent(QueueConnection queueConnection);
    }
}
