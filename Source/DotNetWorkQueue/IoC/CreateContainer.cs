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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.JobScheduler;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Queue;
using SimpleInjector;

namespace DotNetWorkQueue.IoC
{
    internal static class ContainerLocker
    {
        /// <summary>
        /// The locker for creating new containers
        /// </summary>
        public static readonly object Locker = new object();
    }

    /// <summary>
    /// Creates the IoC container
    /// </summary>
    /// <typeparam name="T">The transport registration module</typeparam>
    internal class CreateContainer<T>: ICreateContainer<T> 
        where T : ITransportInit, new()
    {
        /// <summary>
        /// Creates the IoC container
        /// </summary>
        /// <param name="queueType">Type of the queue.</param>
        /// <param name="registerService">The user defined service overrides.</param>
        /// <param name="queue">The queue.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="register">The transport init module.</param>
        /// <param name="connectionType">Type of the connection.</param>
        /// <param name="registerServiceInternal">The internal registrations.</param>
        /// <param name="setOptions">The options.</param>
        /// <param name="registrations">The registrations for job queue creation.</param>
        /// <returns>
        /// a new container
        /// </returns>
        public IContainer Create(QueueContexts queueType, 
            Action<IContainer> registerService,
            QueueConnection queueConnection,
            T register, 
            ConnectionTypes connectionType, 
            Action<IContainer> registerServiceInternal,
            Action<IContainer> setOptions = null,
            JobQueueContainerRegistrations registrations = null)
        {
            lock (ContainerLocker.Locker) //thread safe issue with registration of decorators; should not be needed, but have found no other solution
            {
                var container = new Container();
                container.Options.ResolveUnregisteredConcreteTypes = true;
                var containerWrapper = new ContainerWrapper(container);

                containerWrapper.Register(() => new QueueContext(queueType), LifeStyles.Singleton);
                if (registrations != null)
                {
                    containerWrapper.Register(() => registrations, LifeStyles.Singleton);
                }
                else
                {
                    containerWrapper.Register(() => new JobQueueContainerRegistrations(null, null, null, null), LifeStyles.Singleton);
                }

                var type = GetRegistrationType(register);

                if (!string.IsNullOrWhiteSpace(queueConnection.Queue) && !string.IsNullOrWhiteSpace(queueConnection.Connection))
                {
                    ComponentRegistration.RegisterDefaults(containerWrapper, type);
                }
                else
                {
                    if (queueType == QueueContexts.JobScheduler)
                    {
                        ComponentRegistration.RegisterDefaultsForJobScheduler(containerWrapper);
                    }
                    else
                    {
                        ComponentRegistration.RegisterDefaultsForScheduler(containerWrapper);
                    }
                }

                //allow creating internal queues
                containerWrapper.Register<IQueueContainer>(() => new QueueContainer<T>(),
                    LifeStyles.Singleton);

                // Enable overriding
                container.Options.AllowOverridingRegistrations = true;

                //register transport specific objects
                register.RegisterImplementations(containerWrapper, type, queueConnection);

                //register our internal overrides from outside this container
                registerServiceInternal(containerWrapper);

                //register caller overrides
                registerService(containerWrapper);

                //register conditional fall backs
                container.Options.AllowOverridingRegistrations = false;
                ComponentRegistration.RegisterFallbacks(containerWrapper, type);

                //disable auto verify - we will verify below
                container.Options.EnableAutoVerification = false;

                //allow specific warnings to be disabled
                register.SuppressWarningsIfNeeded(containerWrapper, type);

                //suppress IoC warnings that we are explicitly handling
                ComponentRegistration.SuppressWarningsIfNeeded(containerWrapper, type);

                //set the log provider, if one was provided
                //if no explicit log provider was set, we will use lib log defaults
                var logProvider = container.GetInstance<ILogProvider>();
                if (!(logProvider is NoSpecifiedLogProvider))
                {
                    LogProvider.SetCurrentLogProvider(logProvider);
                    var factory = container.GetInstance<ILogFactory>();
                    factory.Create();
                }

                //verify the container configuration.
                container.Verify();

                //default polices
                ComponentRegistration.SetupDefaultPolicies(containerWrapper, type);

                //allow the transport to set defaults if needed
                register.SetDefaultsIfNeeded(containerWrapper, type, connectionType);

                //allow user override or setting of additional options
                setOptions?.Invoke(containerWrapper);

                return containerWrapper;
            }
        }

        /// <summary>
        /// Creates the IoC container
        /// </summary>
        /// <param name="queueType">Type of the queue.</param>
        /// <param name="registerService">The user defined service overrides.</param>
        /// <param name="register">The transport init module.</param>
        /// <param name="registerServiceInternal">The internal registrations.</param>
        /// <param name="setOptions">The options.</param>
        /// <param name="registrations">The registrations for job queue creation.</param>
        /// <returns>
        /// a new container
        /// </returns>
        public IContainer Create(QueueContexts queueType, Action<IContainer> registerService, T register, Action<IContainer> registerServiceInternal, Action<IContainer> setOptions = null, JobQueueContainerRegistrations registrations = null)
        {
            return Create(queueType, registerService, new QueueConnection(string.Empty, string.Empty), register, ConnectionTypes.NotSpecified, registerServiceInternal, setOptions, registrations);
        }

        /// <summary>
        /// Gets the type of the registration.
        /// </summary>
        /// <param name="registration">The registration.</param>
        /// <returns>
        /// Default, Send, Receive
        /// </returns>
        /// <exception cref="DotNetWorkQueueException">A transport init module should inherit from ITransportInitSend, ITransportInitReceive or ITransportInitDuplex</exception>
        private RegistrationTypes GetRegistrationType(ITransportInit registration)
        {
            if (registration is ITransportInitDuplex ||
                registration is ITransportInitReceive && registration is ITransportInitSend)
            {
                return RegistrationTypes.Send | RegistrationTypes.Receive;
            }
            if (registration is ITransportInitReceive)
            {
                return RegistrationTypes.Receive;
            }
            if (registration is ITransportInitSend)
            {
                return RegistrationTypes.Send;
            }
            throw new DotNetWorkQueueException(
                "A transport init module should inherit from ITransportInitSend, ITransportInitReceive or ITransportInitDuplex");
        }
    }
}
