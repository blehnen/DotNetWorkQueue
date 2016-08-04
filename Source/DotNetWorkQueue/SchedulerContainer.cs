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
using System;
using System.Collections.Concurrent;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.TaskScheduling;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue
{
    #region Scheduler / task factory creation
    /// <summary>
    /// Allows creation of new <see cref="ATaskScheduler"/> and <see cref="ITaskFactory"/> instances
    /// </summary>
    public class SchedulerContainer : BaseContainer
    {
        private static Func<ICreateContainer<SchedulerInit>> _createContainerInternal = () => new CreateContainer<SchedulerInit>();

        private readonly Action<IContainer> _registerService;
        private readonly Action<IContainer> _setOptions;
        private readonly SchedulerInit _transportInit;

        /// <summary>
        /// Set the container creation function. This allows you to use your own IoC container.
        /// </summary>
        public static void SetContainerFactory(Func<ICreateContainer<SchedulerInit>> createContainer)
        {
            Guard.NotNull(() => createContainer, createContainer);
            _createContainerInternal = createContainer;
        }

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="SchedulerContainer"/> class.
        /// </summary>
        public SchedulerContainer()
            : this(x => { })
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref=" SchedulerContainer" /> class.
        /// </summary>
        /// <param name="registerService">The register service.</param>
        /// <param name="setOptions">The options.</param>
        public SchedulerContainer(Action<IContainer> registerService, Action<IContainer> setOptions = null)
        {
            _registerService = registerService;
            _setOptions = setOptions;
            _transportInit = new SchedulerInit();
        }

        #endregion

        #region Task Scheduler

        /// <summary>
        /// Creates the task scheduler.
        /// </summary>
        /// <returns></returns>
        public ATaskScheduler CreateTaskScheduler()
        {
            var container = _createContainerInternal().Create(QueueContexts.TaskScheduler, _registerService, _transportInit, x => { }, _setOptions);
            Containers.Add(container);
            return container.GetInstance<ATaskScheduler>();
        }

        /// <summary>
        /// Creates the task factory.
        /// </summary>
        /// <returns></returns>
        public ITaskFactory CreateTaskFactory()
        {
            var container = _createContainerInternal().Create(QueueContexts.TaskFactory, _registerService, _transportInit,  x => { }, _setOptions);
            Containers.Add(container);
            return CreateTaskFactoryInternal(container.GetInstance<ATaskScheduler>());
        }

        /// <summary>
        /// Creates the task factory.
        /// </summary>
        /// <param name="scheduler">The scheduler.</param>
        /// <returns></returns>
        public ITaskFactory CreateTaskFactory(ATaskScheduler scheduler)
        {
            return CreateTaskFactoryInternal(scheduler);
        }

        /// <summary>
        /// Creates the task factory.
        /// </summary>
        /// <param name="scheduler">The scheduler.</param>
        /// <returns></returns>
        private ITaskFactory CreateTaskFactoryInternal(ATaskScheduler scheduler)
        {
            var container = _createContainerInternal().Create(QueueContexts.TaskFactory, _registerService, _transportInit,
                serviceRegister => serviceRegister.Register(() => scheduler, LifeStyles.Singleton), _setOptions);
            Containers.Add(container);
            return container.GetInstance<ITaskFactory>();
        }

        #endregion
    }

    #endregion
}
