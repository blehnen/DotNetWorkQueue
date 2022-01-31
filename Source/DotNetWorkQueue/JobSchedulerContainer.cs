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
using System;
using System.Collections.Concurrent;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.JobScheduler;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Allows creation of new <see cref="IJobScheduler"/> instances
    /// </summary>
    public class JobSchedulerContainer : IDisposable
    {
        private static Func<ICreateContainer<JobSchedulerInit>> _createContainerInternal = () => new CreateContainer<JobSchedulerInit>();

        private readonly Action<IContainer> _registerService;
        private readonly Action<IContainer> _setOptions;
        private readonly JobSchedulerInit _transportInit;
        private readonly ConcurrentBag<IDisposable> _containers;

        /// <summary>
        /// Set the container creation function. This allows you to use your own IoC container.
        /// </summary>
        public static void SetContainerFactory(Func<ICreateContainer<JobSchedulerInit>> createContainer)
        {
            Guard.NotNull(() => createContainer, createContainer);
            _createContainerInternal = createContainer;
        }


        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="JobSchedulerContainer"/> class.
        /// </summary>
        public JobSchedulerContainer()
            : this(x => { })
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JobSchedulerContainer"/> class.
        /// </summary>
        /// <param name="registerService">The register service.</param>
        /// <param name="setOptions">The set options.</param>
        public JobSchedulerContainer(Action<IContainer> registerService, Action<IContainer> setOptions = null)
        {
            _containers = new ConcurrentBag<IDisposable>();
            _registerService = registerService;
            _setOptions = setOptions;
            _transportInit = new JobSchedulerInit();
        }
        #endregion

        #region Dispose
        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            while (!_containers.IsEmpty)
            {
                if (_containers.TryTake(out var item))
                {
                    item?.Dispose();
                }
            }
        }

        #endregion

        /// <summary>Creates a re-occurring job scheduler.</summary>
        /// <param name="queueCreation">Service registrations for the queue creation modules that will create any needed job queues.</param>
        /// <param name="queueContainer">Service registrations for the queue containers that will contain the producing queues for the jobs.</param>
        /// <param name="queueCreationOptions">allows setting options for queue creation module</param>
        /// <param name="queueContainerOptions">allows setting options for queue container module</param>
        /// <returns></returns>
        public IJobScheduler CreateJobScheduler(Action<IContainer> queueCreation = null, Action<IContainer> queueContainer = null,
            Action<IContainer> queueCreationOptions = null, Action<IContainer> queueContainerOptions = null)
        {
            var container = _createContainerInternal().Create(QueueContexts.JobScheduler, _registerService, _transportInit, x => { }, _setOptions, new JobQueueContainerRegistrations(queueCreation, queueContainer, queueCreationOptions, queueContainerOptions));
            _containers.Add(container);
            return container.GetInstance<IJobScheduler>();
        }
    }
}
