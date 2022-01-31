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
using System;
using System.Collections.Generic;
using System.Reflection;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.LiteDb.Basic.CommandHandler;
using DotNetWorkQueue.Transport.LiteDb.Basic.Factory;
using DotNetWorkQueue.Transport.LiteDb.Basic.Message;
using DotNetWorkQueue.Transport.LiteDb.Basic.Query;
using DotNetWorkQueue.Transport.LiteDb.Basic.QueryHandler;
using DotNetWorkQueue.Transport.LiteDb.Trace.Decorator;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.LiteDb.Basic
{
    /// <summary>
    /// Registers the implementations for the SQL server queue into the IoC container.
    /// </summary>
    public class LiteDbMessageQueueInit : TransportMessageQueueSharedInit
    {
        /// <summary>
        /// Allows a transport to register its dependencies in the IoC container.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="registrationType">Type of the registration.</param>
        /// <param name="queueConnection">Queue and connection information.</param>
        public override void RegisterImplementations(IContainer container, RegistrationTypes registrationType, QueueConnection queueConnection)
        {
            Guard.NotNull(() => container, container);
            base.RegisterImplementations(container, registrationType, queueConnection);

            //**all
            container.RegisterNonScopedSingleton<ICreationScope>(new CreationScope());

            container.Register<IConnectionInformation>(() => new LiteDbConnectionInformation(queueConnection), LifeStyles.Singleton);
            container.Register<IJobSchedulerLastKnownEvent, LiteDbJobSchedulerLastKnownEvent>(LifeStyles.Singleton);
            container.Register<ISendJobToQueue, LiteDbSendJobToQueue>(LifeStyles.Singleton);
            container.Register<DatabaseExists>(LifeStyles.Singleton);
            container.Register<LiteDbMessageQueueSchema>(LifeStyles.Singleton);
            container.Register<LiteDbConnectionManager>(LifeStyles.Singleton);

            container.Register<IJobTableCreation, LiteDbJobTableCreation>(LifeStyles.Singleton);

            container.Register<IQueueCreation, LiteDbMessageQueueCreation>(LifeStyles.Singleton);
            container.Register<TableNameHelper>(LifeStyles.Singleton);
            container.Register<LiteDbMessageQueueTransportOptions>(LifeStyles.Singleton);
            container.Register<IGetPreviousMessageErrors, LiteDbGetPreviousMessageErrors>(LifeStyles.Singleton);

            container.Register<IRemoveMessage, RemoveMessage<int>>(LifeStyles.Singleton);

            container.Register<IGetFirstMessageDeliveryTime, LiteDbGetFirstMessageDeliveryTime>(LifeStyles.Singleton);
            container.Register<IGetHeader, GetHeader<int>>(LifeStyles.Singleton);

            container.Register<IClearExpiredMessages, ClearExpiredMessages<int>>(LifeStyles.Singleton);
            container.Register<IClearErrorMessages, ClearErrorMessages<int>>(LifeStyles.Singleton);
            container.Register<ICorrelationIdFactory, LiteDbCorrelationIdFactory>(LifeStyles.Singleton);
            container.Register<IGetFileNameFromConnectionString, LiteDbGetFileNameFromConnectionString>(LifeStyles.Singleton);
            container.Register<ILiteDbMessageQueueTransportOptionsFactory, LiteDbMessageQueueTransportOptionsFactory>(LifeStyles.Singleton);
            container.Register<IIncreaseQueueDelay, IncreaseQueueDelay>(LifeStyles.Singleton);
            container.Register<IJobSchema, LiteDbJobSchema>(LifeStyles.Singleton);
            container.Register<CreateJobMetaData>(LifeStyles.Singleton);
            //**all

            //**send
            container.Register<ISendMessages, SendMessages<int>>(LifeStyles.Singleton);

            container.Register<ITransportRollbackMessage, LiteDbRollbackMessage>(LifeStyles.Singleton);
            //**send


            //**receive
            container.Register<MessageDeQueue>(LifeStyles.Singleton);
            container.Register<IReceiveMessages, LiteDbQueueReceiveMessages>(LifeStyles.Transient);
            container.Register<ReceiveMessage>(LifeStyles.Singleton);
            container.Register<RollbackMessage>(LifeStyles.Singleton);

            container.Register<IResetHeartBeat, ResetHeartBeat<int>>(LifeStyles.Singleton);
            container.Register<ISendHeartBeat, SendHeartBeat<int>>(LifeStyles.Singleton);
            container.Register<IReceiveMessagesFactory, LiteDbReceiveMessagesFactory>(LifeStyles.Singleton);
            container.Register<IReceivePoisonMessage, ReceivePoisonMessage<int>>(LifeStyles.Singleton);
            container.Register<IReceiveMessagesError, ReceiveErrorMessage<int>>(LifeStyles.Singleton);
            //**receive

            //commands
            var target = Assembly.GetAssembly(GetType());

            container.Register(typeof(ICommandHandlerWithOutput<,>), LifeStyles.Singleton,
                target);

            container.Register(typeof(ICommandHandlerWithOutputAsync<,>), LifeStyles.Singleton,
                target);

            container.Register(typeof(ICommandHandler<>), LifeStyles.Singleton,
                target);

            container.Register(typeof(IQueryHandler<,>), LifeStyles.Singleton,
                target);

            //explicit registration of options
            container
                .Register<IQueryHandler<GetQueueOptionsQuery<LiteDbMessageQueueTransportOptions>,
                        LiteDbMessageQueueTransportOptions>,
                    GetQueueOptionsQueryHandler<LiteDbMessageQueueTransportOptions>>(LifeStyles.Singleton);

            container
                .Register<IQueryHandler<GetMessageErrorsQuery<int>, Dictionary<string, int>>,
                    GetMessageErrorsQueryHandler>(LifeStyles.Singleton);

            container
                .Register<IQueryHandler<GetHeaderQuery<int>, IDictionary<string, object>>,
                    GetHeaderQueryHandler>(LifeStyles.Singleton);

            container
                .Register<IQueryHandler<FindExpiredMessagesToDeleteQuery<int>, IEnumerable<int>>,
                    FindExpiredRecordsToDeleteQueryHandler>(LifeStyles.Singleton);

            container
                .Register<IQueryHandler<FindMessagesToResetByHeartBeatQuery<int>, IEnumerable<MessageToReset<int>>>,
                    FindRecordsToResetByHeartBeatQueryHandler>(LifeStyles.Singleton);

            container
                .Register<IQueryHandler<FindErrorMessagesToDeleteQuery<int>, IEnumerable<int>>,
                    FindErrorRecordsToDeleteQueryHandler>(LifeStyles.Singleton);

            container
                .Register<IQueryHandler<GetErrorRetryCountQuery<int>, int>,
                GetErrorRetryCountQueryHandler>(LifeStyles.Singleton);

            container
                .Register<ICommandHandler<SetErrorCountCommand<int>>,
                    SetErrorCountCommandHandler>(LifeStyles.Singleton);

            //trace fallback command
            container.RegisterDecorator(
                typeof(ICommandHandler<RollbackMessageCommand<int>>),
                typeof(RollbackMessageCommandHandlerDecorator), LifeStyles.Singleton);

            //trace sending a message so that we can add specific tags
            container.RegisterDecorator(
                typeof(ICommandHandlerWithOutput<SendMessageCommand, int>),
                typeof(SendMessageCommandHandlerDecorator), LifeStyles.Singleton);

            container.RegisterDecorator(
                typeof(ICommandHandlerWithOutputAsync<SendMessageCommand, int>),
                typeof(SendMessageCommandHandlerAsyncDecorator), LifeStyles.Singleton);
        }

        /// <summary>
        /// Allows the transport to set default configuration settings or other values
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="registrationType">Type of the registration.</param>
        /// <param name="connectionType">Type of the connection.</param>
        public override void SetDefaultsIfNeeded(IContainer container, RegistrationTypes registrationType, ConnectionTypes connectionType)
        {
            //direct (or memory) connection
            //create in memory hold
            var connection = container.GetInstance<LiteDbConnectionManager>();
            if (!connection.IsSharedConnection)
            {
                var scope = container.GetInstance<ICreationScope>();
                scope.AddScopedObject(connection);
            }

            var factory = container.GetInstance<LiteDbMessageQueueTransportOptionsFactory>();
            var options = factory.Create();
            var configurationSend = container.GetInstance<QueueProducerConfiguration>();
            var configurationReceive = container.GetInstance<QueueConsumerConfiguration>();

            configurationSend.AdditionalConfiguration.SetSetting("LiteDBMessageQueueTransportOptions", options);
            configurationReceive.AdditionalConfiguration.SetSetting("LiteDBMessageQueueTransportOptions", options);

            var transportReceive = container.GetInstance<TransportConfigurationReceive>();


            transportReceive.HeartBeatSupported = options.EnableHeartBeat && options.EnableStatus;
            transportReceive.MessageExpirationSupported = options.EnableMessageExpiration;

            transportReceive.MessageRollbackSupported = options.EnableStatus;

            transportReceive.QueueDelayBehavior.Clear();
            transportReceive.QueueDelayBehavior.Add(DefaultQueueDelay());
            transportReceive.FatalExceptionDelayBehavior.Clear();
            transportReceive.FatalExceptionDelayBehavior.Add(ExceptionDelay());

            transportReceive.LockFeatures();

            SetupHeartBeat(container);
            SetupMessageExpiration(container);
        }

        /// <summary>
        /// Gets the default fatal exception delay time spans
        /// </summary>
        /// <returns></returns>
        private IEnumerable<TimeSpan> ExceptionDelay()
        {
            var rc = new List<TimeSpan>(10)
            {
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(3),
                TimeSpan.FromSeconds(8),
                TimeSpan.FromSeconds(15),
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(60)
            };
            return rc;
        }

        /// <summary>
        /// Gets the default queue delay time spans
        /// </summary>
        /// <returns></returns>
        private IEnumerable<TimeSpan> DefaultQueueDelay()
        {
            var rc = new List<TimeSpan>(21)
            {
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(500),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(1000),
                TimeSpan.FromMilliseconds(2000)
            };
            return rc;
        }

        /// <summary>
        /// Setup the heart beat.
        /// </summary>
        /// <param name="container">The container.</param>
        private void SetupHeartBeat(IContainer container)
        {
            var heartBeatConfiguration = container.GetInstance<IHeartBeatConfiguration>();
            heartBeatConfiguration.Time = TimeSpan.FromSeconds(30);
            heartBeatConfiguration.MonitorTime = TimeSpan.FromSeconds(120);
            heartBeatConfiguration.UpdateTime = "sec(*%10)";
            heartBeatConfiguration.ThreadPoolConfiguration.ThreadsMax = 1;
            heartBeatConfiguration.ThreadPoolConfiguration.WaitForThreadPoolToFinish = TimeSpan.FromSeconds(5);
        }
        /// <summary>
        /// Setup the message expiration.
        /// </summary>
        /// <param name="container">The container.</param>
        private void SetupMessageExpiration(IContainer container)
        {
            var configuration = container.GetInstance<IMessageExpirationConfiguration>();
            configuration.MonitorTime = TimeSpan.FromMinutes(1);
            configuration.Enabled = true;
        }
    }
}
