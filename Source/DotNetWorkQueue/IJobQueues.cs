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
    /// Sends jobs from the scheduler to a specific transport
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    /// <seealso cref="DotNetWorkQueue.IIsDisposed" />
    public interface IJobQueue: IDisposable, IIsDisposed
    {
        /// <summary>
        /// Gets the specified queue.
        /// </summary>
        /// <typeparam name="TTransportInit">The type of the transport initialize.</typeparam>
        /// <typeparam name="TQueue">The type of the queue.</typeparam>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="producerConfiguration">The producer configuration.</param>
        /// <returns></returns>
        IProducerMethodJobQueue Get<TTransportInit, TQueue>(string queue, string connection, Action<QueueProducerConfiguration> producerConfiguration = null)
            where TTransportInit : ITransportInit, new()
            where TQueue : class, IJobQueueCreation;

        /// <summary>
        /// Gets the specified queue.
        /// </summary>
        /// <typeparam name="TTransportInit">The type of the transport initialize.</typeparam>
        /// <param name="jobQueueCreation">The job queue creation.</param>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="producerConfiguration">The producer configuration.</param>
        /// <returns></returns>
        IProducerMethodJobQueue Get<TTransportInit>(IJobQueueCreation jobQueueCreation, string queue, string connection, Action<QueueProducerConfiguration> producerConfiguration = null)
            where TTransportInit : ITransportInit, new();
    }
}
