// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
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
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using DotNetWorkQueue.Exceptions;
using SimpleInjector;
using SimpleInjector.Diagnostics;
namespace DotNetWorkQueue.IoC
{
    /// <summary>
    /// The limited IoC container provided for user code / transport inits
    /// </summary>
    /// <remarks>This exists so that outside code does not need to reference simple injector directly.</remarks>
    public class ContainerWrapper : IContainer
    {
        private Container _container;
        private int _disposeCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerWrapper" /> class.
        /// </summary>
        /// <param name="container">The container.</param>
        public ContainerWrapper(Container container)
        {
            Guard.NotNull(() => container, container);
            _container = container;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is verifying.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is verifying; otherwise, <c>false</c>.
        /// </value>
        public bool IsVerifying => _container.IsVerifying;

        /// <summary>
        /// Provides access to the underlying container
        /// </summary>
        /// <value>
        /// The container.
        /// </value>
        /// <remarks>Intended to aid unit tests, though can be accessed by anything if needed.</remarks>
        public dynamic Container => _container;


        #region IDispose, IIsDisposed
        /// <summary>
        /// Throws an exception if this instance has been disposed.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <exception cref="System.ObjectDisposedException"></exception>
        protected void ThrowIfDisposed([CallerMemberName] string name = "")
        {
            if (Interlocked.CompareExchange(ref _disposeCount, 0, 0) != 0)
            {
                throw new ObjectDisposedException(name);
            }
        }
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

            if (Interlocked.Increment(ref _disposeCount) != 1) return;

            _container.Dispose();
            _container = null;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is disposed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is disposed; otherwise, <c>false</c>.
        /// </value>
        public bool IsDisposed => Interlocked.CompareExchange(ref _disposeCount, 0, 0) != 0;
        #endregion

        /// <summary>
        /// Returns the specified instance.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <returns></returns>
        public TService GetInstance<TService>() where TService : class
        {
            return _container.GetInstance<TService>();
        }

        /// <summary>
        /// Returns the specified instance based on the input service type
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <returns></returns>
        public object GetInstance(Type serviceType)
        {
            return _container.GetInstance(serviceType);
        }

        /// <summary>
        /// Registers the service with the specified life style.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation.</typeparam>
        /// <param name="lifeStyle">The life style.</param>
        /// <returns></returns>
        public IContainer Register<TService, TImplementation>(LifeStyles lifeStyle)
            where TService : class
            where TImplementation : class, TService
        {
            _container.Register<TService, TImplementation>(GetLifeStyle(lifeStyle));
            return this;
        }

        /// <summary>
        /// Registers the service with the specified life style.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <param name="instanceCreator">The instance creator.</param>
        /// <param name="lifeStyle">The life style.</param>
        /// <returns></returns>
        public IContainer Register<TService>(Func<TService> instanceCreator, LifeStyles lifeStyle)
            where TService : class
        {
            _container.Register(instanceCreator, GetLifeStyle(lifeStyle));
            return this;
        }

        /// <summary>
        /// Registers the implementation type as a fallback if no other registration has been made
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="implementationType">Type of the implementation.</param>
        /// <param name="lifestyle">The lifestyle.</param>
        /// <returns></returns>
        public IContainer RegisterConditional(Type serviceType, Type implementationType, LifeStyles lifestyle)
        {
            _container.RegisterConditional(serviceType, implementationType, GetLifeStyle(lifestyle), c => !c.Handled);
            return this;
        }

        /// <summary>
        /// Registers the service with the specified life style.
        /// </summary>
        /// <typeparam name="TConcrete">The type of the concrete implementation.</typeparam>
        /// <param name="lifeStyle">The life style.</param>
        /// <returns></returns>
        public IContainer Register<TConcrete>(LifeStyles lifeStyle) where TConcrete : class
        {
            _container.Register<TConcrete>(GetLifeStyle(lifeStyle));
            return this;
        }

        /// <summary>
        /// Registers the specified service type.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="instanceCreator">The instance creator.</param>
        /// <param name="lifestyle">The lifestyle.</param>
        /// <returns></returns>
        public IContainer Register(Type serviceType, Func<object> instanceCreator, LifeStyles lifestyle)
        {
            _container.Register(serviceType, instanceCreator, GetLifeStyle(lifestyle));
            return this;
        }

        /// <summary>
        /// Registers the specified service type and implementation
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="implementationType">Type of the implementation.</param>
        /// <param name="lifestyle">The lifestyle.</param>
        /// <returns></returns>
        public IContainer Register(Type serviceType, Type implementationType, LifeStyles lifestyle)
        {
            _container.Register(serviceType, implementationType, GetLifeStyle(lifestyle));
            return this;
        }

        /// <summary>
        /// Registers multiple service types with the specified life style
        /// </summary>
        /// <param name="openGenericServiceType">Type of the open generic service.</param>
        /// <param name="lifeStyle">The life style.</param>
        /// <param name="assemblies">The assemblies.</param>
        /// <returns></returns>
        public IContainer Register(Type openGenericServiceType, LifeStyles lifeStyle,
            params Assembly[] assemblies)
        {
            _container.Register(openGenericServiceType, assemblies, GetLifeStyle(lifeStyle));
            return this;
        }

        /// <summary>
        /// Registers a collection of services for a single service type
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <param name="serviceTypes">The service types.</param>
        /// <returns></returns>
        public IContainer RegisterCollection<TService>(IEnumerable<Type> serviceTypes) 
            where TService : class
        {
            _container.RegisterCollection<TService>(serviceTypes);
            return this;
        }

        /// <summary>
        /// Registers a decorator for the indicated service type.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="decoratorType">Type of the decorator.</param>
        /// <param name="lifestyle">The lifestyle.</param>
        /// <returns></returns>
        public IContainer RegisterDecorator(Type serviceType, Type decoratorType, LifeStyles lifestyle)
        {
            _container.RegisterDecorator(serviceType, decoratorType, GetLifeStyle(lifestyle));
            return this;
        }

        /// <summary>
        ///  Ensures that the supplied TDecorator decorator is returned and cached with the given lifestyle
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <typeparam name="TDecorator">The type of the decorator.</typeparam>
        /// <param name="lifestyle">The lifestyle.</param>
        /// <returns></returns>
        public IContainer RegisterDecorator<TService, TDecorator>(LifeStyles lifestyle)
        {
            _container.RegisterDecorator<TService, TDecorator>(GetLifeStyle(lifestyle));
            return this;
        }

        /// <summary>
        /// Suppress the diagnostic warning for a service type and warning type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="warningType">Type of the warning.</param>
        /// <param name="reason">The reason.</param>
        /// <returns></returns>
        /// <remarks>
        /// Sometimes, the warnings are a little too aggressive. For instance, the container can't tell that you are using a using() {} statement on a <see cref="IDisposable"/>; it flags this as a possible problem.
        /// </remarks>
        public IContainer SuppressDiagnosticWarning(Type type, DiagnosticTypes warningType, string reason)
        {
            var target = _container.GetRegistration(type);
            if (target == null) return this;
            var registration = _container.GetRegistration(type).Registration;
            registration?.SuppressDiagnosticWarning((DiagnosticType) warningType, reason);
            return this;
        }

        /// <summary>
        /// Gets simple injector life style based on the internal enum.
        /// </summary>
        /// <param name="lifeStyle">The life style.</param>
        /// <returns></returns>
        /// <exception cref="DotNetWorkQueueException"></exception>
        private Lifestyle GetLifeStyle(LifeStyles lifeStyle)
        {
            switch (lifeStyle)
            {
                case LifeStyles.Singleton:
                    return Lifestyle.Singleton;
                case LifeStyles.Transient:
                    return Lifestyle.Transient;
                default:
                    throw new DotNetWorkQueueException($"unknown life style type {lifeStyle}");
            }
        }
    }
}
