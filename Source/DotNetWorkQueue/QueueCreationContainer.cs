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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IoC;
namespace DotNetWorkQueue
{
    #region Creating Queue in transport
    /// <summary>
    /// Creates a module for creating queues.
    /// </summary>
    /// <typeparam name="T">The type of transport to use</typeparam>
    public class QueueCreationContainer<T> : IDisposable
        where T : ITransportInit, new()
    {
        // ReSharper disable once StaticMemberInGenericType
        private static Func<ICreateContainer<T>> _createContainerInternal = () => new CreateContainer<T>();
        private readonly ConcurrentBag<IDisposable> _containers;

        #region Static IoC replacement functions
        /// <summary>
        /// Sets the container creation function. This allows you to use your own IoC container, instead of the built in one.
        /// </summary>
        public static void SetContainerFactory(Func<ICreateContainer<T>> createContainer)
        {
            Guard.NotNull(() => createContainer, createContainer);
            _createContainerInternal = createContainer;
        }
        #endregion

        private readonly Action<IContainer> _registerService;
        private readonly Action<IContainer> _setOptions;
        private readonly T _transportInit;

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueCreationContainer{T}"/> class.
        /// </summary>
        public QueueCreationContainer()
            : this(x => { })
        {

        }
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueCreationContainer{T}" /> class.
        /// </summary>
        /// <param name="registerService">Override the default services.</param>
        /// <param name="setOptions">The options.</param>
        public QueueCreationContainer(Action<IContainer> registerService, Action<IContainer> setOptions = null)
        {
            _containers = new ConcurrentBag<IDisposable>();
            _registerService = registerService;
            _setOptions = setOptions;
            _transportInit = new T();
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

        #region Queue Creation

        /// <summary>
        /// Gets the requested module for creating a queue.
        /// </summary>
        /// <typeparam name="TQueue">Module to use</typeparam>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <returns></returns>
        public TQueue GetQueueCreation<TQueue>(string queue, string connection) where TQueue : class, IQueueCreation
        {
            Guard.NotNullOrEmpty(() => queue, queue);
            Guard.NotNullOrEmpty(() => connection, connection);

            var container = _createContainerInternal().Create(QueueContexts.QueueCreator, _registerService, queue, connection, _transportInit,
                ConnectionTypes.Send, x => { }, _setOptions);
            lock (_containers)
            {
                _containers.Add(container);
            }
            return container.GetInstance<TQueue>();
        }
        #endregion
    }
    #endregion

    #region Creating Queue in transport
    /// <summary>
    /// Creates a module for creating queues.
    /// </summary>
    /// <typeparam name="T">The type of transport to use</typeparam>
    public class JobQueueCreationContainer<T> : IDisposable
        where T : ITransportInit, new()
    {
        // ReSharper disable once StaticMemberInGenericType
        private static Func<ICreateContainer<T>> _createContainerInternal = () => new CreateContainer<T>();
        private readonly ConcurrentBag<IDisposable> _containers;
        private readonly Action<IContainer> _setOptions;

        #region Static IoC replacement functions
        /// <summary>
        /// Sets the container creation function. This allows you to use your own IoC container, instead of the built in one.
        /// </summary>
        public static void SetContainerFactory(Func<ICreateContainer<T>> createContainer)
        {
            Guard.NotNull(() => createContainer, createContainer);
            _createContainerInternal = createContainer;
        }
        #endregion

        private readonly Action<IContainer> _registerService;
        private readonly T _transportInit;

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueCreationContainer{T}"/> class.
        /// </summary>
        public JobQueueCreationContainer()
            : this(x => { })
        {

        }
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueCreationContainer{T}" /> class.
        /// </summary>
        /// <param name="registerService">Override the default services.</param>
        /// <param name="setOptions">The options.</param>
        public JobQueueCreationContainer(Action<IContainer> registerService, Action<IContainer> setOptions = null)
        {
            _containers = new ConcurrentBag<IDisposable>();
            _registerService = registerService;
            _setOptions = setOptions;
            _transportInit = new T();
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

        #region Queue Creation

        /// <summary>
        /// Gets the requested module for creating a queue.
        /// </summary>
        /// <typeparam name="TQueue">Module to use</typeparam>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <returns></returns>
        public TQueue GetQueueCreation<TQueue>(string queue, string connection) where TQueue : class, IJobQueueCreation
        {
            Guard.NotNullOrEmpty(() => queue, queue);
            Guard.NotNullOrEmpty(() => connection, connection);

            var container = _createContainerInternal().Create(QueueContexts.JobQueueCreator, _registerService, queue, connection, _transportInit,
                ConnectionTypes.Send, x => { }, _setOptions);
            lock (_containers)
            {
                _containers.Add(container);
            }
            return container.GetInstance<TQueue>();
        }
        #endregion
    }
    #endregion
}
