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
using System.Collections.Generic;
using System.Reflection;

namespace DotNetWorkQueue
{
    /// <summary>
    /// An interface for the IoC container
    /// </summary>
    /// <remarks>The default container is SimpleInjector - this interface is a subset of the simple injector features</remarks>
    public interface IContainer: IDisposable, IIsDisposed
    {
        /// <summary>
        /// Gets a value indicating whether this instance is verifying.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is verifying; otherwise, <c>false</c>.
        /// </value>
        bool IsVerifying { get; }

        /// <summary>
        /// Returns the specified instance.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <returns></returns>
        TService GetInstance<TService>() where TService : class;

        /// <summary>
        /// Returns the specified instance based on the input service type
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <returns></returns>
        object GetInstance(Type serviceType);

        /// <summary>
        /// Registers the service with the specified life style.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation.</typeparam>
        /// <param name="lifeStyle">The life style.</param>
        /// <returns></returns>
        IContainer Register<TService, TImplementation>(LifeStyles lifeStyle)
            where TService : class
            where TImplementation : class, TService;

        /// <summary>
        /// Registers the specified service type and implementation
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="implementationType">Type of the implementation.</param>
        /// <param name="lifestyle">The lifestyle.</param>
        /// <returns></returns>
        IContainer Register(Type serviceType, Type implementationType, LifeStyles lifestyle);

        /// <summary>
        /// Registers the service with the specified life style.
        /// </summary>
        /// <typeparam name="TConcrete">The type of the concrete implementation.</typeparam>
        /// <param name="lifeStyle">The life style.</param>
        /// <returns></returns>
        IContainer Register<TConcrete>(LifeStyles lifeStyle) where TConcrete : class;

        /// <summary>
        /// Registers the service with the specified life style.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <param name="instanceCreator">The instance creator.</param>
        /// <param name="lifeStyle">The life style.</param>
        /// <returns></returns>
        IContainer Register<TService>(Func<TService> instanceCreator, LifeStyles lifeStyle) where TService : class;

        /// <summary>
        /// Registers multiple service types with the specified life style
        /// </summary>
        /// <param name="openGenericServiceType">Type of the open generic service.</param>
        /// <param name="lifeStyle">The life style.</param>
        /// <param name="assemblies">The assemblies.</param>
        /// <returns></returns>
        IContainer Register(Type openGenericServiceType, LifeStyles lifeStyle, params Assembly[] assemblies);

        /// <summary>
        /// Registers a singleton that will not be scoped and disposed of with the container.
        /// </summary>
        /// <typeparam name="TConcrete">The type of the concrete.</typeparam>
        /// <param name="instance">The instance.</param>
        /// <returns></returns>
        IContainer RegisterNonScopedSingleton<TConcrete>(TConcrete instance)
            where TConcrete : class;

        /// <summary>
        /// Registers a decorator for the indicated service type.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="decoratorType">Type of the decorator.</param>
        /// <param name="lifestyle">The lifestyle.</param>
        /// <returns></returns>
        IContainer RegisterDecorator(Type serviceType, Type decoratorType, LifeStyles lifestyle);

        /// <summary>
        ///  Ensures that the supplied TDecorator decorator is returned and cached with the given lifestyle
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <typeparam name="TDecorator">The type of the decorator.</typeparam>
        /// <param name="lifestyle">The lifestyle.</param>
        /// <returns></returns>
        IContainer RegisterDecorator<TService, TDecorator>(LifeStyles lifestyle)
            where TService : class
            where TDecorator : class, TService;

        /// <summary>
        /// Registers the specified service type.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="instanceCreator">The instance creator.</param>
        /// <param name="lifestyle">The lifestyle.</param>
        /// <returns></returns>
        IContainer Register(Type serviceType, Func<object> instanceCreator, LifeStyles lifestyle);

        /// <summary>
        /// Registers the implementation type as a fall back if no other registration has been made
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="implementationType">Type of the implementation.</param>
        /// <param name="lifestyle">The lifestyle.</param>
        /// <returns></returns>
        IContainer RegisterConditional(Type serviceType, Type implementationType, LifeStyles lifestyle);

        /// <summary>
        /// Registers the implementation type as a fall back if no other registration has been made.
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <typeparam name="TImplementation">The type of the implementation.</typeparam>
        /// <param name="lifestyle">The lifestyle.</param>
        /// <returns></returns>
        IContainer RegisterConditional<TService, TImplementation>(LifeStyles lifestyle)
            where TService : class
            where TImplementation : class, TService;

        /// <summary>
        /// Registers a collection of services for a single service type
        /// </summary>
        /// <typeparam name="TService">The type of the service.</typeparam>
        /// <param name="serviceTypes">The service types.</param>
        /// <returns></returns>
        IContainer RegisterCollection<TService>(IEnumerable<Type> serviceTypes) 
            where TService : class;

        /// <summary>
        /// Suppress the diagnostic warning for a service type and warning type.
        /// </summary>
        /// <remarks>Sometimes, the warnings are a little too aggressive. For instance, the container can't tell that you are using a using() {} statement on a disposable; it flags this as a possible problem.</remarks>
        /// <param name="type">The type.</param>
        /// <param name="warningType">Type of the warning.</param>
        /// <param name="reason">The reason.</param>
        /// <returns></returns>
        /// <remarks>Containers that do not implement diagnostic warnings may noOp this method</remarks>
        IContainer SuppressDiagnosticWarning(Type type, DiagnosticTypes warningType, string reason);

        /// <summary>
        /// Provides access to the underlying container
        /// </summary>
        /// <value>
        /// The container.
        /// </value>
        /// <remarks>Intended to aid unit tests, though can be accessed by anything if needed.</remarks>
        dynamic Container { get; }

        /// <summary>
        /// Adds the type that needs a warning suppression.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <remarks>Adding an exception for a type that was not actually registered in the container causes all sorts of problems</remarks>
        void AddTypeThatNeedsWarningSuppression(Type type);

        /// <summary>
        /// Gets the types that can be suppressed.
        /// </summary>
        HashSet<Type> TypesThatCanBeSuppressed
        {
            get;
        }
    }

    /// <summary>
    /// The supported IoC lifestyles
    /// </summary>
    public enum LifeStyles
    {
        /// <summary>
        /// Every call to GetInstance will return a new object
        /// </summary>
        Transient = 0,
        /// <summary>
        /// Every call to GetInstance will return the same object
        /// </summary>
        Singleton = 1
    }
    /// <summary>
    ///  Specifies the list of diagnostic types that are currently supported by the Analyzer.
    /// </summary>
    public enum DiagnosticTypes
    {
        /// <summary>
        /// A container registered component
        /// </summary>
        ContainerRegisteredComponent = 0,
        /// <summary>
        /// lifestyle mismatch
        /// </summary>
        LifestyleMismatch = 1,
        /// <summary>
        /// The short circuited dependency
        /// </summary>
        ShortCircuitedDependency = 2,
        /// <summary>
        /// The single responsibility violation
        /// </summary>
        SingleResponsibilityViolation = 3,
        /// <summary>
        /// torn lifestyle
        /// </summary>
        TornLifestyle = 4,
        /// <summary>
        /// disposable transient component
        /// </summary>
        DisposableTransientComponent = 5,
        /// <summary>
        /// ambiguous lifestyles
        /// </summary>
        AmbiguousLifestyles = 6
    }
}
