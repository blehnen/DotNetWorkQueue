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
using DotNetWorkQueue.QueueStatus;
namespace DotNetWorkQueue
{
    /// <summary>
    /// Allows creating a queue status module <see cref="IQueueStatus"/>
    /// </summary>
    public class QueueStatusContainer: IDisposable
    {
        private static Func<ICreateContainer<QueueStatusInit>> _createContainerInternal = () => new CreateContainer<QueueStatusInit>();

        private readonly Action<IContainer> _registerService;
        private readonly Action<IContainer> _setOptions;
        private readonly QueueStatusInit _transportInit;
        private readonly ConcurrentBag<IDisposable> _containers;

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
            _containers = new ConcurrentBag<IDisposable>();
            _registerService = registerService;
            _setOptions = setOptions;
            _transportInit = new QueueStatusInit();
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
            lock (_containers)
            {
                while (!_containers.IsEmpty)
                {
                    IDisposable item;
                    if (_containers.TryTake(out item))
                    {
                        item?.Dispose();
                    }
                }
            }
        }
        #endregion

        /// <summary>
        /// Creates a new instance of <see cref="IQueueStatus" />
        /// </summary>
        /// <returns></returns>
        public IQueueStatus CreateStatus()
        {
            //when creating a status module outside of the queue itself, use a noop system information module
            var container = _createContainerInternal().Create(QueueContexts.QueueStatus ,_registerService, _transportInit,
                x => { }, _setOptions);

            _containers.Add(container);
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
            _containers.Add(creator);
            return creator.CreateStatusProvider(queue, connection);
        }
    }
}
