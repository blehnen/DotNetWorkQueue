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
using System.Linq;
using System.Reflection;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Redis.Basic.Factory;
using DotNetWorkQueue.Transport.Redis.Basic.Lua;
using DotNetWorkQueue.Transport.Redis.Basic.Message;
using DotNetWorkQueue.Transport.Redis.Basic.MessageID;
using DotNetWorkQueue.Transport.Redis.Basic.Metrics.Decorator;
using DotNetWorkQueue.Transport.Redis.Basic.Query;
using DotNetWorkQueue.Transport.Redis.Basic.Time;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// Adds redis implementations to the Ioc Container
    /// </summary>
    public class RedisQueueInit: TransportMessageQueueSharedInit
    {
        /// <summary>
        /// Allows a transport to register its dependencies in the IoC container.
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="registrationType">Type of the registration.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="queue">The queue.</param>
        public override void RegisterImplementations(IContainer container, RegistrationTypes registrationType, string connection, string queue)
        {
            Guard.NotNull(() => container, container);
            base.RegisterImplementations(container, registrationType, connection, queue);

            //**all
            container.Register<IConnectionInformation>(() => new RedisConnectionInfo(queue, connection), LifeStyles.Singleton);
            container.Register<IJobSchedulerLastKnownEvent, RedisJobSchedulerLastKnownEvent>(LifeStyles.Singleton);
            container.Register<ISendJobToQueue, RedisSendJobToQueue>(LifeStyles.Singleton);

            container.Register<IJobTableCreation, RedisJobTableCreationNoOp>(LifeStyles.Singleton);

            container.Register<IQueueCreation, RedisQueueCreation>(LifeStyles.Singleton);
            container.Register<IRedisConnection, RedisConnection>(LifeStyles.Singleton);
            container.Register<RedisNames>(LifeStyles.Singleton);
            container.Register<RedisQueueTransportOptions>(LifeStyles.Singleton);
            container.Register<DelayedProcessingConfiguration>(LifeStyles.Singleton);
            container.Register<ICorrelationIdFactory, RedisQueueCorrelationIdFactory>(
                LifeStyles.Singleton);
            container.Register<IGetPreviousMessageErrors, RedisGetPreviousMessageErrors>(LifeStyles.Singleton);

            container.Register<IGetHeader, GetHeader>(LifeStyles.Singleton);

            container.Register<IRemoveMessage, RemoveMessage>(LifeStyles.Singleton);
            container.Register<ICreationScope, CreationScopeNoOp>(LifeStyles.Singleton);

            container.Register<IGetFirstMessageDeliveryTime, GetFirstMessageDeliveryTime>(LifeStyles.Singleton);
            container.Register<IGetTimeFactory, GetRedisTimeFactory>(LifeStyles.Singleton);

            container.Register<LuaScripts>(LifeStyles.Singleton);

            container.Register<IUnixTimeFactory, UnixTimeFactory>(LifeStyles.Singleton);
            
            container.Register<LocalMachineUnixTime>(LifeStyles.Singleton);
            container.Register<RedisServerUnixTime>(LifeStyles.Singleton);
            container.Register<SntpUnixTime>(LifeStyles.Singleton);

            container.Register<SntpTimeConfiguration>(LifeStyles.Singleton);
            container.Register<RedisHeaders>(LifeStyles.Singleton);
            container.Register<IClearExpiredMessages, RedisQueueClearExpiredMessages>(LifeStyles.Singleton);
            container.Register<IClearErrorMessages, RedisQueueClearErrorMessages>(LifeStyles.Singleton);
            //**all

            //**send
            container.Register<ISendMessages, RedisQueueSend>(LifeStyles.Singleton);

            container.Register<ITransportRollbackMessage, RollbackMessage>(LifeStyles.Singleton);

            container.Register<IGetMessageIdFactory, GetMessageIdFactory>(LifeStyles.Singleton);
            container.Register<GetUuidMessageId>(LifeStyles.Singleton);
            container.Register<GetRedisIncrId>(LifeStyles.Singleton);

            container.Register<ISendBatchSize, RedisSimpleBatchSize>(LifeStyles.Singleton);
            //**send


            //**receive
            container.Register<IDelayedProcessingAction, DelayedProcessingAction>(LifeStyles.Singleton);
            container.Register<IQueueMonitor, RedisQueueMonitor>(LifeStyles.Singleton);

            container.Register<IReceiveMessages, RedisQueueReceiveMessages>(LifeStyles.Transient);

            container.Register<IDelayedProcessingMonitor, RedisDelayedProcessingMonitor>(LifeStyles.Singleton);
            container.Register<IResetHeartBeat, RedisQueueResetHeartBeat>(LifeStyles.Singleton);
            container.Register<ISendHeartBeat, RedisQueueSendHeartBeat>(LifeStyles.Singleton);
            container.Register<IReceiveMessagesFactory, ReceiveMessagesFactory>(LifeStyles.Singleton);
            container.Register<IReceivePoisonMessage, RedisQueueReceivePoisonMessage>(LifeStyles.Singleton);
            container.Register<IRedisQueueWorkSubFactory, RedisQueueWorkSubFactory>(LifeStyles.Singleton);
            container.Register<IReceiveMessagesError, RedisQueueReceiveMessagesError>(LifeStyles.Singleton);
            //**receive

            //commands
            var target = Assembly.GetAssembly(GetType());

            container.Register(typeof(ICommandHandlerWithOutput<,>), LifeStyles.Singleton,
                target);

            container.Register(typeof(ICommandHandler<>), LifeStyles.Singleton,
                target);

            container.Register(typeof(IQueryHandler<,>), LifeStyles.Singleton,
                target);

            //LUA scripts
            container.Register<DequeueLua>(LifeStyles.Singleton);
            container.Register<EnqueueLua>(LifeStyles.Singleton);
            container.Register<EnqueueDelayedLua>(LifeStyles.Singleton);
            container.Register<EnqueueExpirationLua>(LifeStyles.Singleton);
            container.Register<EnqueueDelayedAndExpirationLua>(LifeStyles.Singleton);
            container.Register<DeleteLua>(LifeStyles.Singleton);
            container.Register<RollbackLua>(LifeStyles.Singleton);
            container.Register<RollbackDelayLua>(LifeStyles.Singleton);
            container.Register<ErrorLua>(LifeStyles.Singleton);
            container.Register<MoveDelayedToPendingLua>(LifeStyles.Singleton);
            container.Register<ResetHeartbeatLua>(LifeStyles.Singleton);
            container.Register<TimeLua>(LifeStyles.Singleton);
            container.Register<EnqueueBatchLua>(LifeStyles.Singleton);
            container.Register<DoesJobExistLua>(LifeStyles.Singleton);
            container.Register<GetHeaderLua>(LifeStyles.Singleton);

            var types = target.GetTypes().Where(x => x.IsSubclassOf(typeof(BaseLua)));
            container.RegisterCollection<BaseLua>(types);

            //metric decorators
            container.RegisterDecorator<IDelayedProcessingAction, DelayedProcessingActionDecorator>(LifeStyles.Singleton);
            container.RegisterDecorator<IQueryHandler<ReceiveMessageQuery, RedisMessage>, ReceiveMessageQueryDecorator>(
                LifeStyles.Singleton);

            //logging decorators
            container.RegisterDecorator<IQueryHandler<ReceiveMessageQuery, RedisMessage>, Logging.Decorator.ReceiveMessageQueryDecorator>(
                LifeStyles.Singleton);
            container.RegisterDecorator<IDelayedProcessingAction, Logging.Decorator.DelayedProcessingActionDecorator>(LifeStyles.Singleton);

            //trace fallback command
            container.RegisterDecorator(
                typeof(ICommandHandler<RollbackMessageCommand>),
                typeof(DotNetWorkQueue.Transport.Redis.Trace.Decorator.RollbackMessageCommandHandlerDecorator), LifeStyles.Singleton);

            //trace sending messages
            container.RegisterDecorator(
                typeof(ISendMessages),
                typeof(DotNetWorkQueue.Transport.Redis.Trace.Decorator.SendMessagesDecorator), LifeStyles.Singleton);
        }

        /// <summary>
        /// Allows the transport to set default configuration settings or other values
        /// </summary>
        /// <param name="container">The container.</param>
        /// <param name="registrationType">Type of the registration.</param>
        /// <param name="connectionType">Type of the connection.</param>
        public override void SetDefaultsIfNeeded(IContainer container, RegistrationTypes registrationType, ConnectionTypes connectionType)
        {
            var options = container.GetInstance<RedisQueueTransportOptions>();
            var configurationSend = container.GetInstance<QueueProducerConfiguration>();
            var configurationReceive = container.GetInstance<QueueConsumerConfiguration>();

            configurationSend.AdditionalConfiguration.SetSetting("RedisQueueTransportOptions", options);
            configurationReceive.AdditionalConfiguration.SetSetting("RedisQueueTransportOptions", options);

            var transportReceive = container.GetInstance<TransportConfigurationReceive>();
            transportReceive.HeartBeatSupported = true;
            transportReceive.MessageExpirationSupported = true;
            transportReceive.MessageRollbackSupported = true;

            transportReceive.QueueDelayBehavior.Clear();
            transportReceive.QueueDelayBehavior.Add(DefaultQueueDelay());
            transportReceive.FatalExceptionDelayBehavior.Clear();
            transportReceive.FatalExceptionDelayBehavior.Add(ExceptionDelay());

            options.TimeServer = TimeLocations.RedisServer;

            transportReceive.LockFeatures();

            SetupHeartBeat(container);
            SetupMessageExpiration(container);

            //only compile scripts if the container is not in verification mode
            if (!container.IsVerifying)
            {
                SetupScripts(container);
            }
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
            //we use pub/sub to determine if work needs to be done - no delay here is fine
            return new List<TimeSpan>(0);
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

        /// <summary>
        /// Pre-compiles all LUA scripts
        /// </summary>
        /// <param name="container">The container.</param>
        private void SetupScripts(IContainer container)
        {
            container.GetInstance<LuaScripts>().Setup();
        }
    }
}
