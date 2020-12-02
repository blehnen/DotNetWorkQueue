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
using DotNetWorkQueue.JobScheduler;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Creates new instance of <see cref="IContainer"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ICreateContainer<in T> 
        where T : ITransportInit, new()
    {
        /// <summary>
        /// Creates the IoC container
        /// </summary>
        /// <param name="queueType">Type of the queue.</param>
        /// <param name="registerService">The user defined service overrides</param>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="register">The transport registration module.</param>
        /// <param name="connectionType">Type of the connection.</param>
        /// <param name="registerServiceInternal">The internal service overrides</param>
        /// <param name="setOptions">The options. Can be null</param>
        /// <param name="registrations">The registrations for job queue creation. Can be null</param>
        /// <returns></returns>
        IContainer Create(QueueContexts queueType, Action<IContainer> registerService, QueueConnection queueConnection, T register,
            ConnectionTypes connectionType, Action<IContainer> registerServiceInternal, Action<IContainer> setOptions, JobQueueContainerRegistrations registrations = null);

        /// <summary>
        /// Creates the IoC container
        /// </summary>
        /// <param name="queueType">Type of the queue.</param>
        /// <param name="registerService">The user defined service overrides.</param>
        /// <param name="register">The transport registration module.</param>
        /// <param name="registerServiceInternal">The internal service overrides.</param>
        /// <param name="setOptions">The options. Can be null</param>
        /// <param name="registrations">The registrations for job queue creation. Can be null</param>
        /// <returns></returns>
        IContainer Create(QueueContexts queueType, Action<IContainer> registerService, T register,
            Action<IContainer> registerServiceInternal, Action<IContainer> setOptions, JobQueueContainerRegistrations registrations = null);
    }
}
