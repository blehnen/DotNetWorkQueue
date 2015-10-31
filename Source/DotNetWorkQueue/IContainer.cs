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
        IContainer RegisterDecorator<TService, TDecorator>(LifeStyles lifestyle);

        /// <summary>
        /// Registers the specified service type.
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="instanceCreator">The instance creator.</param>
        /// <param name="lifestyle">The lifestyle.</param>
        /// <returns></returns>
        IContainer Register(Type serviceType, Func<object> instanceCreator, LifeStyles lifestyle);

        /// <summary>
        /// Registers the implementation type as a fallback if no other registration has been made
        /// </summary>
        /// <param name="serviceType">Type of the service.</param>
        /// <param name="implementationType">Type of the implementation.</param>
        /// <param name="lifestyle">The lifestyle.</param>
        /// <returns></returns>
        IContainer RegisterConditional(Type serviceType, Type implementationType, LifeStyles lifestyle);

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
        ///     Diagnostic type that warns about a concrete type that was not registered
        ///     explicitly and was not resolved using unregistered type resolution, but was
        ///     created by the container using the transient lifestyle.
        /// </summary>
        ContainerRegisteredComponent = 0,
        /// <summary>
        ///  Diagnostic type that warns when a component depends on a service with a lifestyle
        ///     that is shorter than that of the component.
        /// </summary>
        LifestyleMismatch = 1,
        /// <summary>
        ///  Diagnostic type that warns when a component depends on an unregistered concrete
        ///     type and this concrete type has a lifestyle that is different than the lifestyle
        ///     of an explicitly registered type that uses this concrete type as its implementation.
        /// </summary>
        ShortCircuitedDependency = 3,
        /// <summary>
        /// Diagnostic type that warns when a component depends on (too) many services.
        /// </summary>
        SingleResponsibilityViolation = 4,
        /// <summary>
        ///  Diagnostic type that warns when multiple registrations map to the same component
        ///     and lifestyle, which might cause multiple instances to be created during
        ///     the lifespan of that lifestyle.
        /// </summary>
        TornLifestyle = 5,
        /// <summary>
        ///  Diagnostic type that warns when a component is registered as transient, while
        ///     implementing System.IDisposable.
        /// </summary>
        DisposableTransientComponent = 6,
        /// <summary>
        ///  Diagnostic type that warns when multiple registrations exist that map to
        ///     the same component but with different lifestyles, which will cause the component
        ///     to be cached in different -possibly incompatible- ways.
        /// </summary>
        AmbiguousLifestyles = 7,
    }
}
