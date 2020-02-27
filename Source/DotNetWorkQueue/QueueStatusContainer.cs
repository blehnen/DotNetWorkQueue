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
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.QueueStatus;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Allows creating a queue status module <see cref="IQueueStatus"/>
    /// </summary>
    public class QueueStatusContainer: BaseContainer
    {
        private static Func<ICreateContainer<QueueStatusInit>> _createContainerInternal = () => new CreateContainer<QueueStatusInit>();

        private readonly Action<IContainer> _registerService;
        private readonly Action<IContainer> _setOptions;
        private readonly QueueStatusInit _transportInit;

        #region Static IoC replacement functions
        /// <summary>
        /// Set the container creation function. This allows you to use your own IoC container.
        /// </summary>
        public static void SetContainerFactory(Func<ICreateContainer<QueueStatusInit>> createContainer)
        {
            Guard.NotNull(() => createContainer, createContainer);
            _createContainerInternal = createContainer;
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueStatusContainer"/> class.
        /// </summary>
        public QueueStatusContainer()
            : this(x => { })
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueStatusContainer" /> class.
        /// </summary>
        /// <param name="registerService">The register service.</param>
        /// <param name="setOptions">The options.</param>
        public QueueStatusContainer(Action<IContainer> registerService,  Action<IContainer> setOptions = null)
        {
            _registerService = registerService;
            _setOptions = setOptions;
            _transportInit = new QueueStatusInit();
        }

        #endregion

        /// <summary>
        /// Creates a new instance of <see cref="IQueueStatus" />
        /// </summary>
        /// <returns></returns>
        public IQueueStatus CreateStatus()
        {
            //when creating a status module outside of the queue itself, use a no-op system information module
            var container = _createContainerInternal().Create(QueueContexts.QueueStatus ,_registerService, _transportInit,
                x => { }, _setOptions);

            Containers.Add(container);
            return container.GetInstance<IQueueStatus>();
        }

        /// <summary>
        /// Creates a new status provider.
        /// </summary>
        /// <typeparam name="TTransportInit">The type of the transport initialize.</typeparam>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <returns></returns>
        public IQueueStatusProvider CreateStatusProvider<TTransportInit>(string queue, string connection)
            where TTransportInit : ITransportInit, new()
        {
            return CreateStatusProvider<TTransportInit>(x => { }, queue, connection);
        }


        /// <summary>
        /// Creates a new status provider.
        /// </summary>
        /// <returns></returns>
        public IQueueStatusProvider CreateStatusProvider<TTransportInit>(Action<IContainer> registerService,
            string queue, string connection)
                where TTransportInit : ITransportInit, new()
        {
            var creator = new QueueContainer<TTransportInit>(registerService);
            Containers.Add(creator);
            return creator.CreateStatusProvider(queue, connection);
        }
    }
}
