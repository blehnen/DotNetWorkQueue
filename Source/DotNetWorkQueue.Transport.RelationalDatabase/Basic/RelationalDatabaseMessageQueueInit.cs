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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandHandler;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.CommandPrepareHandler;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryPrepareHandler;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Basic.Factory;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    /// <summary>
    /// Sets default implementions in the container
    /// </summary>
    public class RelationalDatabaseMessageQueueInit<TQueueId, TCorrelationId>
        where TQueueId : struct, IComparable<TQueueId>
        where TCorrelationId: struct, IComparable<TCorrelationId>

    {
        /// <summary>
        /// Registers the standard implementations.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="caller">The caller.</param>
        public void RegisterStandardImplementations(IContainer container, params Assembly[] caller)
        {
            Guard.NotNull(() => container, container);

            //**all
            container.Register<IJobTableCreation, JobTableCreation>(LifeStyles.Singleton);
            container.Register<CreateJobMetaData>(LifeStyles.Singleton);
            container.Register<IReadColumn, ReadColumn>(LifeStyles.Singleton);
            container.Register<ITransactionFactory, TransactionFactory>(LifeStyles.Singleton);
            container.Register<ICreationScope, CreationScopeNoOp>(LifeStyles.Singleton);
            container.Register<ICorrelationIdFactory, CorrelationIdFactory<TCorrelationId>>(
                LifeStyles.Singleton);

            container.Register<ITableNameHelper, TableNameHelper>(LifeStyles.Singleton);
            container.Register<TableNameHelper>(LifeStyles.Singleton);

            container.Register<IClearExpiredMessages, ClearExpiredMessages<TQueueId>>(LifeStyles.Singleton);
            container.Register<IClearErrorMessages, ClearErrorMessages<TQueueId>>(LifeStyles.Singleton);
            container.Register<IRemoveMessage, RemoveMessage<TQueueId>>(LifeStyles.Singleton);
            container.Register<IGetHeader, GetHeader<TQueueId>>(LifeStyles.Singleton);
            //**all

            //**send
            container.Register<ISendMessages, SendMessages<TQueueId>>(LifeStyles.Singleton);
            //**send


            //**receive
            container.Register<IGetColumnsFromTable, GetColumnsFromTable>(LifeStyles.Singleton);
            container.Register<IResetHeartBeat, ResetHeartBeat<TQueueId>>(LifeStyles.Singleton);
            container.Register<ISendHeartBeat, SendHeartBeat<TQueueId>>(LifeStyles.Singleton);
            container.Register<IReceiveMessagesFactory, ReceiveMessagesFactory>(LifeStyles.Singleton);
            container.Register<IReceivePoisonMessage, ReceivePoisonMessage<TQueueId>>(LifeStyles.Singleton);
            container.Register<IReceiveMessagesError, ReceiveErrorMessage<TQueueId>>(LifeStyles.Singleton);
            container.Register<IIncreaseQueueDelay, IncreaseQueueDelay>(LifeStyles.Singleton);
            //**receive

            RegisterCommands(container, caller);
            RegisterCommands(container, Assembly.GetAssembly(GetType()));
            RegisterCommandsExplicit(container);
        }

        /// <summary>
        /// Sets the defaults if needed.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="configurationSendName">Name of the configuration send.</param>
        /// <param name="configurationReceiveName">Name of the configuration receive.</param>
        public void SetDefaultsIfNeeded(IContainer container, string configurationSendName, string configurationReceiveName)
        {
            var factory = container.GetInstance<ITransportOptionsFactory>();
            var options = factory.Create();
            var configurationSend = container.GetInstance<QueueProducerConfiguration>();
            var configurationReceive = container.GetInstance<QueueConsumerConfiguration>();

            configurationSend.AdditionalConfiguration.SetSetting(configurationSendName, options);
            configurationReceive.AdditionalConfiguration.SetSetting(configurationReceiveName, options);

            var transportReceive = container.GetInstance<TransportConfigurationReceive>();


            transportReceive.HeartBeatSupported = options.EnableHeartBeat && options.EnableStatus &&
                                                  !options.EnableHoldTransactionUntilMessageCommitted;

            transportReceive.MessageExpirationSupported = options.EnableMessageExpiration;

            transportReceive.MessageRollbackSupported = options.EnableStatus ||
                                                        options.EnableHoldTransactionUntilMessageCommitted;

            transportReceive.QueueDelayBehavior.Clear();
            transportReceive.QueueDelayBehavior.Add(DefaultQueueDelay.GetDefaultQueueDelay());
            transportReceive.FatalExceptionDelayBehavior.Clear();
            transportReceive.FatalExceptionDelayBehavior.Add(ExceptionDelay.GetExceptionDelay());

            transportReceive.LockFeatures();

            SetupHeartBeat(container);
            SetupMessageExpiration(container);
        }

        /// <summary>
        /// Setup the heart beat.
        /// </summary>
        /// <param name="container">The container.</param>
        public void SetupHeartBeat(IContainer container)
        {
            var heartBeatConfiguration = container.GetInstance<IHeartBeatConfiguration>();
            if (!heartBeatConfiguration.Enabled) return;
            heartBeatConfiguration.Time = TimeSpan.FromSeconds(600);
            heartBeatConfiguration.MonitorTime = TimeSpan.FromMinutes(3);
            heartBeatConfiguration.UpdateTime = "min(*%2)";
            heartBeatConfiguration.ThreadPoolConfiguration.ThreadsMax = 1;
            heartBeatConfiguration.ThreadPoolConfiguration.WaitForThreadPoolToFinish = TimeSpan.FromSeconds(5);
        }

        /// <summary>
        /// Setup the message expiration.
        /// </summary>
        /// <param name="container">The container.</param>
        public void SetupMessageExpiration(IContainer container)
        {
            var configuration = container.GetInstance<IMessageExpirationConfiguration>();
            if (!configuration.Supported) return;
            configuration.MonitorTime = TimeSpan.FromMinutes(3);
            configuration.Enabled = true;
        }

        private void RegisterCommandsExplicit(IContainer container)
        {
            //Type implementation =
            //    typeof(IQueryHandler<,>).MakeGenericType(
            //        typeof(IQuery<TQueueId>),
            //        typeof(IQueryHandler<,>).GetGenericArguments()[1]);

            //container.Register(typeof(IQueryHandler<,>), new List<Type>(){ implementation}, LifeStyles.Singleton);

            container
                .Register<IQueryHandler<GetMessageErrorsQuery<TQueueId>, Dictionary<string, int>>,
                    GetMessageErrorsQueryHandler<TQueueId>>(LifeStyles.Singleton);

            container
                .Register<IPrepareQueryHandler<GetMessageErrorsQuery<TQueueId>, Dictionary<string, int>>,
                    GetMessageErrorsQueryPrepareHandler<TQueueId>>(LifeStyles.Singleton);

            container
                .Register<IPrepareQueryHandler<GetHeaderQuery<TQueueId>, IDictionary<string, object>>,
                    GetHeaderQueryPrepareHandler<TQueueId>>(LifeStyles.Singleton);

            container
                .Register<IQueryHandler<GetHeaderQuery<TQueueId>, IDictionary<string, object>>,
                    GetHeaderQueryHandler<TQueueId>>(LifeStyles.Singleton);

            container
                .Register<IQueryHandler<GetJobIdQuery<TQueueId>, TQueueId>,
                    GetJobIdQueryHandler<TQueueId>>(LifeStyles.Singleton);

            container
                .Register<IPrepareQueryHandler<GetJobIdQuery<TQueueId>, TQueueId>,
                    GetJobIdQueryPrepareHandler<TQueueId>>(LifeStyles.Singleton);

            container
                .Register<IQueryHandler<FindExpiredMessagesToDeleteQuery<TQueueId>, IEnumerable<TQueueId>>,
                    FindExpiredRecordsToDeleteQueryHandler<TQueueId>>(LifeStyles.Singleton);

            container
                .Register<IPrepareQueryHandler<FindExpiredMessagesToDeleteQuery<TQueueId>, IEnumerable<TQueueId>>,
                    FindExpiredRecordsToDeleteQueryPrepareHandler<TQueueId>>(LifeStyles.Singleton);

            container
                .Register<IQueryHandler<FindMessagesToResetByHeartBeatQuery<TQueueId>, IEnumerable<MessageToReset<TQueueId>>>,
                    FindRecordsToResetByHeartBeatQueryHandler<TQueueId>>(LifeStyles.Singleton);

            container
                .Register<IPrepareQueryHandler<FindMessagesToResetByHeartBeatQuery<TQueueId>, IEnumerable<MessageToReset<TQueueId>>>,
                    FindRecordsToResetByHeartBeatQueryPrepareHandler<TQueueId>>(LifeStyles.Singleton);

            container
                .Register<IQueryHandler<FindErrorMessagesToDeleteQuery<TQueueId>, IEnumerable<TQueueId>>,
                    FindErrorRecordsToDeleteQueryHandler<TQueueId>>(LifeStyles.Singleton);

            container
                .Register<IPrepareQueryHandler<FindErrorMessagesToDeleteQuery<TQueueId>, IEnumerable<TQueueId>>,
                    FindErrorRecordsToDeleteQueryPrepareHandler<TQueueId>>(LifeStyles.Singleton);

            container
                .Register<IQueryHandler<GetErrorRetryCountQuery<TQueueId>, int>,
                GetErrorRetryCountQueryHandler<TQueueId>>(LifeStyles.Singleton);

            container
                .Register<IPrepareQueryHandler<GetErrorRetryCountQuery<TQueueId>, int>,
                    GetErrorRetryCountQueryPrepareHandler<TQueueId>>(LifeStyles.Singleton);

            container
                .Register<ICommandHandler<SetErrorCountCommand<TQueueId>>,
                    SetErrorCountCommandHandler<TQueueId>>(LifeStyles.Singleton);

            container
                .Register<IPrepareCommandHandler<SetErrorCountCommand<TQueueId>>,
                    SetErrorCountCommandPrepareHandler<TQueueId>>(LifeStyles.Singleton);

            container
                .Register<IQueryHandler<GetErrorRecordExistsQuery<TQueueId>, bool>,
                    GetErrorRecordExistsQueryHandler<TQueueId>>(LifeStyles.Singleton);

            container
                .Register<IPrepareQueryHandler<GetErrorRecordExistsQuery<TQueueId>, bool>,
                    GetErrorRecordExistsQueryPrepareHandler<TQueueId>>(LifeStyles.Singleton);
        }

        private void RegisterCommands(IContainer container, params Assembly[] target)
        {
            //commands and decorators
            // Go look in all assemblies and register all implementations
            // of ICommandHandlerWithOutput<T> by their closed interface:
            container.Register(typeof(ICommandHandlerWithOutput<,>), LifeStyles.Singleton,
                target);

            //commands and decorators
            // Go look in all assemblies and register all implementations
            // of ICommandHandlerWithOutputAsync<T> by their closed interface:
            container.Register(typeof(ICommandHandlerWithOutputAsync<,>), LifeStyles.Singleton,
                target);

            // Go look in all assemblies and register all implementations
            // of ICommandHandler<T> by their closed interface:
            container.Register(typeof(ICommandHandler<>), LifeStyles.Singleton,
                target);

            // Go look in all assemblies and register all implementations
            // of IQueryHandler<T> by their closed interface:
            container.Register(typeof(IQueryHandler<,>), LifeStyles.Singleton,
                target);

            // Go look in all assemblies and register all implementations
            // of ICommandHandler<T> by their closed interface:
            container.Register(typeof(IPrepareCommandHandler<>), LifeStyles.Singleton,
                target);

            // Go look in all assemblies and register all implementations
            // of ICommandHandler<T> by their closed interface:
            container.Register(typeof(IPrepareCommandHandlerWithOutput<,>), LifeStyles.Singleton,
                target);

            // Go look in all assemblies and register all implementations
            // of ICommandHandler<T> by their closed interface:
            container.Register(typeof(IPrepareQueryHandler<,>), LifeStyles.Singleton,
                target);
        }
    }
}
