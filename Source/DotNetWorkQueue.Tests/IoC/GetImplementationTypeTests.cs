// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using DotNetWorkQueue.IoC;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleInjector;

namespace DotNetWorkQueue.Tests.IoC
{
    /// <summary>
    /// <see cref="IContainer.GetImplementationType{TService}"/> is how a factory decides whether it
    /// may build the default implementation itself instead of paying for a resolve.
    /// </summary>
    /// <remarks>
    /// Everything here is about answering "do not know" safely. The factories treat anything other
    /// than the exact default type as a reason to defer to the container, so every case that
    /// cannot be answered has to come back as something that is not that type.
    /// </remarks>
    [TestClass]
    public class GetImplementationTypeTests
    {
        [TestMethod]
        public void Reports_The_Registered_Implementation()
        {
            using var container = NewContainer();
            container.Register<IThing, Thing>(Lifestyle.Transient);

            var test = new ContainerWrapper(container);

            Assert.AreEqual(typeof(Thing), test.GetImplementationType<IThing>());
        }

        [TestMethod]
        public void Reports_The_Service_Type_For_A_Delegate_Registration()
        {
            //this is the case that protects SQL Server and PostgreSQL: both register
            //IWorkerNotification as a delegate that picks a relational variant at resolve time.
            //Reporting anything other than the default implementation is what makes the caller
            //defer to the container rather than build the default itself.
            using var container = NewContainer();
            container.Register<IThing>(() => new Thing(), Lifestyle.Transient);

            var test = new ContainerWrapper(container);

            Assert.AreNotEqual(typeof(Thing), test.GetImplementationType<IThing>());
        }

        [TestMethod]
        public void Returns_Null_For_An_Unregistered_Service()
        {
            using var container = NewContainer();

            var test = new ContainerWrapper(container);

            Assert.IsNull(test.GetImplementationType<IThing>(),
                "an unregistered service is a 'do not know', not an error");
        }

        [TestMethod]
        public void An_Implementation_That_Does_Not_Override_It_Answers_Null()
        {
            //the default interface method. A container implemented outside this library gets it
            //for free and always defers, which is the compatibility promise the release notes make
            IContainer test = new ContainerThatOnlyResolves();

            Assert.IsNull(test.GetImplementationType<IThing>());
        }

        private static Container NewContainer()
        {
            var container = new Container();
            container.Options.EnableAutoVerification = false;
            return container;
        }

        public interface IThing;

        public sealed class Thing : IThing;

        /// <summary>
        /// The smallest thing that is an <see cref="IContainer"/> and does not override
        /// <see cref="IContainer.GetImplementationType{TService}"/>, so the default runs.
        /// </summary>
        private sealed class ContainerThatOnlyResolves : IContainer
        {
            public bool IsVerifying => false;
            public bool IsDisposed => false;
            public dynamic Container => null;
            public HashSet<Type> TypesThatCanBeSuppressed => new HashSet<Type>();

            public TService GetInstance<TService>() where TService : class => throw NotUsed();
            public object GetInstance(Type serviceType) => throw NotUsed();

            public IContainer Register<TService, TImplementation>(LifeStyles lifeStyle)
                where TService : class where TImplementation : class, TService => throw NotUsed();
            public IContainer Register(Type serviceType, Type implementationType, LifeStyles lifestyle) => throw NotUsed();
            public IContainer Register<TConcrete>(LifeStyles lifeStyle) where TConcrete : class => throw NotUsed();
            public IContainer Register<TService>(Func<TService> instanceCreator, LifeStyles lifeStyle)
                where TService : class => throw NotUsed();
            public IContainer Register(Type openGenericServiceType, LifeStyles lifeStyle, params Assembly[] assemblies) => throw NotUsed();
            public IContainer Register(Type openGenericServiceType, IEnumerable<Type> implementationTypes, LifeStyles lifeStyle) => throw NotUsed();
            public IContainer Register(Type serviceType, Func<object> instanceCreator, LifeStyles lifestyle) => throw NotUsed();
            public IContainer RegisterNonScopedSingleton<TConcrete>(TConcrete instance) where TConcrete : class => throw NotUsed();
            public IContainer RegisterDecorator(Type serviceType, Type decoratorType, LifeStyles lifestyle) => throw NotUsed();
            public IContainer RegisterDecorator<TService, TDecorator>(LifeStyles lifestyle)
                where TService : class where TDecorator : class, TService => throw NotUsed();
            public IContainer RegisterConditional(Type serviceType, Type implementationType, LifeStyles lifestyle) => throw NotUsed();
            public IContainer RegisterConditional<TService, TImplementation>(LifeStyles lifestyle)
                where TService : class where TImplementation : class, TService => throw NotUsed();
            public IContainer RegisterCollection<TService>(IEnumerable<Type> serviceTypes) where TService : class => throw NotUsed();
            public IContainer SuppressDiagnosticWarning(Type type, DiagnosticTypes warningType, string reason) => throw NotUsed();
            public void AddTypeThatNeedsWarningSuppression(Type type) => throw NotUsed();
            public void Dispose() { }

            private static NotSupportedException NotUsed() =>
                new NotSupportedException("Only GetImplementationType is under test here.");
        }
    }
}
