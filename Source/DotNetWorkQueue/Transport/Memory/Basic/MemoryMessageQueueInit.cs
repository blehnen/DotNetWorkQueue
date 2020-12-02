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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Transport.Memory.Basic.Factory;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Memory.Basic
{
    /// <summary>
    /// Registers the implementations for the queue into the IoC container.
    /// </summary>
    public class MemoryMessageQueueInit : TransportInitDuplex
    {
        /// <summary>
        /// Registers the implementations.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="registrationType">Type of the registration.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="queue">The queue.</param>
        public override void RegisterImplementations(IContainer container, RegistrationTypes registrationType,
            QueueConnection queueConnection)
        {
            Guard.NotNull(() => container, container);

            container.RegisterNonScopedSingleton<ICreationScope>(new CreationScope());

            //**all
            container.Register<IJobTableCreation, JobTableCreation>(LifeStyles.Singleton);
            container.Register<CreateJobMetaData>(LifeStyles.Singleton);
            container.Register<ICorrelationIdFactory, CorrelationIdFactory>(LifeStyles.Singleton);
            container.Register<IClearExpiredMessages, ClearExpiredMessages>(LifeStyles.Singleton);
            container.Register<IClearErrorMessages, ClearErrorMessages>(LifeStyles.Singleton);
            container.Register<IDataStorage, DataStorage>(LifeStyles.Singleton);
            container.Register<IRemoveMessage, RemoveMessage>(LifeStyles.Singleton);
            container.Register<IGetHeader, GetHeader>(LifeStyles.Singleton);
            container.Register<IGetPreviousMessageErrors, GetPreviousMessageErrorsNoOp>(LifeStyles.Singleton);
            //**all

            //**send
            container.Register<ISendMessages, SendMessages>(LifeStyles.Singleton);
            //**send


            //**receive
            container.Register<IResetHeartBeat, ResetHeartBeat>(LifeStyles.Singleton);
            container.Register<ISendHeartBeat, SendHeartBeat>(LifeStyles.Singleton);
            container.Register<IReceiveMessagesFactory, ReceiveMessagesFactory>(LifeStyles.Singleton);
            container.Register<IReceivePoisonMessage, ReceivePoisonMessage>(LifeStyles.Singleton);
            container.Register<IReceiveMessagesError, ReceiveErrorMessage>(LifeStyles.Singleton);
            //**receive

            //**all
            container.RegisterNonScopedSingleton<ICreationScope>(new CreationScope());

            container.Register<IQueueCreation, MessageQueueCreation>(LifeStyles.Singleton);
            container.Register<IJobSchedulerLastKnownEvent, JobSchedulerLastKnownEvent>(LifeStyles.Singleton);
            container.Register<ISendJobToQueue, SendToJobQueue>(LifeStyles.Singleton);
            container.Register<ITransportOptionsFactory, TransportOptionsFactory>(LifeStyles.Singleton);

            container.Register<IGetFirstMessageDeliveryTime, GetFirstMessageDeliveryTime>(LifeStyles.Singleton);

            container.Register<IConnectionInformation>(() => new ConnectionInformation(queueConnection),
                LifeStyles.Singleton);
          
            container.Register<TransportOptions>(LifeStyles.Singleton);
            //**all

            //**receive
            container.Register<IReceiveMessages, MessageQueueReceive>(LifeStyles.Transient);
            //**receive
        }

        /// <summary>
        /// Allows the transport to set default configuration settings or other values
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="registrationType">Type of the registration.</param>
        /// <param name="connectionType">Type of the connection.</param>
        public override void SetDefaultsIfNeeded(IContainer container, RegistrationTypes registrationType, ConnectionTypes connectionType)
        {
            //create in memory hold
            var scope = container.GetInstance<ICreationScope>();
            var holder = container.GetInstance<IDataStorage>();
            scope.AddScopedObject(holder);

            var factory = container.GetInstance<ITransportOptionsFactory>();
            var options = factory.Create();
            var configurationSend = container.GetInstance<QueueProducerConfiguration>();
            var configurationReceive = container.GetInstance<QueueConsumerConfiguration>();

            configurationSend.AdditionalConfiguration.SetSetting("MemoryTransportOptions", options);
            configurationReceive.AdditionalConfiguration.SetSetting("MemoryTransportOptions", options);

            var transportReceive = container.GetInstance<TransportConfigurationReceive>();

            transportReceive.HeartBeatSupported = false;
            transportReceive.MessageExpirationSupported = false;
            transportReceive.MessageRollbackSupported = false;

            transportReceive.QueueDelayBehavior.Clear();
            transportReceive.QueueDelayBehavior.Add(GetQueueDelay());
            transportReceive.FatalExceptionDelayBehavior.Clear();

            transportReceive.LockFeatures();
        }

        private IEnumerable<TimeSpan> GetQueueDelay()
        {
            return new List<TimeSpan>(0);
        }
    }
}
