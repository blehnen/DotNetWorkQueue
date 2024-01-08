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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Validation;
using System;

namespace DotNetWorkQueue
{
    #region Creating Queue in transport
    /// <summary>
    /// Creates a module for creating queues.
    /// </summary>
    /// <typeparam name="T">The type of transport to use</typeparam>
    public class QueueCreationContainer<T> : BaseContainer
        where T : ITransportInit, new()
    {
        // ReSharper disable once StaticMemberInGenericType
        private static Func<ICreateContainer<T>> _createContainerInternal = () => new CreateContainer<T>();

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
            _registerService = registerService;
            _setOptions = setOptions;
            _transportInit = new T();
        }
        #endregion

        #region Queue Creation

        /// <summary>
        /// Gets the requested module for creating a queue.
        /// </summary>
        /// <typeparam name="TQueue">Module to use</typeparam>
        /// <param name="queueConnection">Queue and connection information.</param>
        /// <returns></returns>
        public TQueue GetQueueCreation<TQueue>(QueueConnection queueConnection) where TQueue : class, IQueueCreation
        {
            ThrowIfDisposed();

            Guard.NotNull(() => queueConnection, queueConnection);
            var container = _createContainerInternal().Create(QueueContexts.QueueCreator, _registerService, queueConnection, _transportInit,
                ConnectionTypes.Send, x => { }, _setOptions);
            lock (Containers)
            {
                Containers.Add(container);
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
    public class JobQueueCreationContainer<T> : BaseContainer
        where T : ITransportInit, new()
    {
        // ReSharper disable once StaticMemberInGenericType
        private static Func<ICreateContainer<T>> _createContainerInternal = () => new CreateContainer<T>();
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
            _registerService = registerService;
            _setOptions = setOptions;
            _transportInit = new T();
        }
        #endregion

        #region Queue Creation

        /// <summary>
        /// Gets the requested module for creating a queue.
        /// </summary>
        /// <typeparam name="TQueue">Module to use</typeparam>
        /// <param name="queueConnection">Queue and connection information.</param>
        /// <returns></returns>
        public TQueue GetQueueCreation<TQueue>(QueueConnection queueConnection) where TQueue : class, IJobQueueCreation
        {
            ThrowIfDisposed();

            Guard.NotNull(() => queueConnection, queueConnection);
            var container = _createContainerInternal().Create(QueueContexts.JobQueueCreator, _registerService, queueConnection, _transportInit,
                ConnectionTypes.Send, x => { }, _setOptions);
            lock (Containers)
            {
                Containers.Add(container);
            }
            return container.GetInstance<TQueue>();
        }
        #endregion
    }
    #endregion
}
