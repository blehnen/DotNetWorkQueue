using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Factory;
using DotNetWorkQueue.IoC;
using DotNetWorkQueue.Messages;
using NSubstitute;
using SimpleInjector;
using SimpleInjector.Diagnostics;
using Xunit;

namespace DotNetWorkQueue.Tests.IoC
{
    [Collection("IoC")]
    public class CreateContainerTest
    {
        [Fact]
        public void CreateContainer_NoWarnings_NoOpSendTransport()
        {
            var creator = new CreateContainer<NoOpSendTransport>();
            var c = creator.Create(QueueContexts.NotSet, x => { }, string.Empty, string.Empty, new NoOpSendTransport(), ConnectionTypes.Send, y => { });

            // Assert
            Container container = c.Container;
            var results = Analyzer.Analyze(container);
            Assert.False(results.Any(), Environment.NewLine +
                                        string.Join(Environment.NewLine,
                                            from result in results
                                            select result.Description));
        }

        [Fact]
        public void CreateContainer_NoWarnings_NoOpReceiveTransport()
        {
            var creator = new CreateContainer<NoOpReceiveTransport>();
            var c = creator.Create(QueueContexts.NotSet, x => { }, string.Empty, string.Empty, new NoOpReceiveTransport(), ConnectionTypes.Receive, y => { });

            // Assert
            Container container = c.Container;
            var results = Analyzer.Analyze(container);
            Assert.False(results.Any(), Environment.NewLine +
                                        string.Join(Environment.NewLine,
                                            from result in results
                                            select result.Description));
        }

        [Fact]
        public void CreateContainer_BadTransport_Exception()
        {
            Assert.Throws<DotNetWorkQueueException>(
              delegate
              {
                  var creator = new CreateContainer<NoOpBadTransport>();
                  creator.Create(QueueContexts.NotSet, x => { }, string.Empty, string.Empty, new NoOpBadTransport(), ConnectionTypes.NotSpecified, y => { });
              });
        }

        internal class NoOpSendTransport : TransportInitSend
        {
            public override void RegisterImplementations(IContainer container, RegistrationTypes registrationType, string connection, string queue)
            {
                container.Register<ISendMessages, SendMessagesNoOp>(LifeStyles.Singleton);
                container.Register(() => Substitute.For<IConnectionInformation>(), LifeStyles.Singleton);
                container.Register(() => Substitute.For<ICorrelationIdFactory>(),
                    LifeStyles.Singleton);
                container.Register<IGetFirstMessageDeliveryTime, GetFirstMessageDeliveryTimeNoOp>(LifeStyles.Singleton);
                container.Register<IInternalSerializer, InternalSerializerNoOp>(LifeStyles.Singleton);
            }
        }

        internal class NoOpReceiveTransport : TransportInitReceive
        {
            public override void RegisterImplementations(IContainer container, RegistrationTypes registrationType,
                string connection, string queue)
            {
                container.Register(() => Substitute.For<IConnectionInformation>(),
                    LifeStyles.Singleton);

                container.Register(() => Substitute.For<ICorrelationIdFactory>(),
                    LifeStyles.Singleton);
                container.Register(
                    () => Substitute.For<IReceiveMessagesFactory>(), LifeStyles.Singleton);
                container.Register(
                    () => Substitute.For<IReceiveMessagesError>(), LifeStyles.Singleton);

                container.Register<IInternalSerializer, InternalSerializerNoOp>(LifeStyles.Singleton);

                container.Register<IWorkerNotificationFactory, WorkerNotificationFactoryNoOp>(LifeStyles.Singleton);
            }
        }

        internal class NoOpDuplexTransport : TransportInitDuplex
        {
            public override void RegisterImplementations(IContainer container, RegistrationTypes registrationType, string connection, string queue)
            {
                container.Register<IConnectionInformation>(() => new BaseConnectionInformation(queue, connection), LifeStyles.Singleton);
                container.Register<ISendMessages, SendMessagesNoOp>(LifeStyles.Singleton);
                container.Register<IGetFirstMessageDeliveryTime, GetFirstMessageDeliveryTimeNoOp>(LifeStyles.Singleton);

                container.Register(() => Substitute.For<ICorrelationIdFactory>(),
                    LifeStyles.Singleton);
                container.Register(
                    () => Substitute.For<IReceiveMessagesFactory>(), LifeStyles.Singleton);

                container.Register(
                    () => Substitute.For<IJobSchedulerLastKnownEvent>(), LifeStyles.Singleton);

                container.Register(
                   () => Substitute.For<ISendJobToQueue>(), LifeStyles.Singleton);

                container.Register(
                  () => Substitute.For<IJobTableCreation>(), LifeStyles.Singleton);

                container.Register<ATaskScheduler, TaskSchedulerNoOp>(LifeStyles.Singleton);
                container.Register<IClearExpiredMessages, ClearExpiredMessagesNoOp>(LifeStyles.Singleton);
                container.Register<ISendHeartBeat, SendHeartBeatNoOp>(LifeStyles.Singleton);
                container.Register<IReceiveMessagesError, ReceiveMessagesErrorNoOp>(LifeStyles.Singleton);
                container.Register<IReceivePoisonMessage, ReceivePoisonMessageNoOp>(LifeStyles.Singleton);
                container.Register<IResetHeartBeat, ResetHeartBeatNoOp>(LifeStyles.Singleton);
            }
        }

        internal class NoOpBadTransport : ITransportInit
        {
            public void RegisterImplementations(IContainer container, RegistrationTypes registrationType, string connection, string queue)
            {
          
            }

            public void SuppressWarningsIfNeeded(IContainer container, RegistrationTypes registrationType)
            {
            
            }

            public void SetDefaultsIfNeeded(IContainer container, RegistrationTypes registrationType, ConnectionTypes connectionType)
            {
                
            }
        }

        internal class InternalSerializerNoOp : IInternalSerializer
        {
            public byte[] ConvertToBytes<T>(T data) where T : class
            {
                throw new NotImplementedException();
            }

            public string ConvertToString<T>(T data) where T : class
            {
                throw new NotImplementedException();
            }

            public T ConvertBytesTo<T>(byte[] bytes) where T : class
            {
                throw new NotImplementedException();
            }
        }
        internal class ResetHeartBeatNoOp : IResetHeartBeat
        {
            public List<ResetHeartBeatOutput> Reset(CancellationToken cancelToken)
            {
                return new List<ResetHeartBeatOutput>(0);
            }
        }

        internal class TaskSchedulerNoOp : ATaskScheduler
        {
            protected override void QueueTask(Task task)
            {
                
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                return false;
            }

            protected override IEnumerable<Task> GetScheduledTasks()
            {
                return Enumerable.Empty<Task>();
            }

            public override void Start()
            {
                
            }

            // ReSharper disable once UnassignedGetOnlyAutoProperty
            public override RoomForNewTaskResult RoomForNewTask { get; }
            public override RoomForNewTaskResult RoomForNewWorkGroupTask(IWorkGroup group)
            {
                return RoomForNewTaskResult.No;
            }

            public override IWorkGroup AddWorkGroup(string name, int concurrencyLevel)
            {
                return null;
            }

            public override IWorkGroup AddWorkGroup(string name, int concurrencyLevel, int maxQueueSize)
            {
                return null;
            }

            public override void AddTask(Task task)
            {
               
            }

            // ReSharper disable once UnassignedGetOnlyAutoProperty
            public override ITaskSchedulerConfiguration Configuration { get; }
            // ReSharper disable once UnassignedGetOnlyAutoProperty
            public override IWaitForEventOrCancelThreadPool WaitForFreeThread { get; }

            public override int Subscribe()
            {
                return 1;
            }

            public override void UnSubscribe(int id)
            {
                
            }

            // ReSharper disable once UnassignedGetOnlyAutoProperty
            public override bool Started => true;

            protected override void Dispose(bool disposing)
            {
               
            }

            // ReSharper disable once UnassignedGetOnlyAutoProperty
            public override bool IsDisposed { get; }
        }
        internal class ReceivePoisonMessageNoOp : IReceivePoisonMessage
        {
            public void Handle(IMessageContext context, PoisonMessageException exception)
            {
                
            }
        }
        internal class ReceiveMessagesErrorNoOp : IReceiveMessagesError
        {
            public ReceiveMessagesErrorResult MessageFailedProcessing(IReceivedMessageInternal message, IMessageContext context,
                Exception exception)
            {
                return ReceiveMessagesErrorResult.NotSpecified;
            }
        }
        internal class ClearExpiredMessagesNoOp : IClearExpiredMessages
        {
            public long ClearMessages(CancellationToken cancelToken)
            {
                return 0;
            }
        }

        internal class SendHeartBeatNoOp : ISendHeartBeat
        {
            public IHeartBeatStatus Send(IMessageContext context)
            {
                return null;
            }
        }
        internal class GetFirstMessageDeliveryTimeNoOp : IGetFirstMessageDeliveryTime
        {
            public DateTime GetTime(IMessage message, IAdditionalMessageData data)
            {
                return DateTime.UtcNow;
            }
        }
        internal class SendMessagesNoOp : ISendMessages
        {
            public IQueueOutputMessage Send(IMessage messageToSend, IAdditionalMessageData data)
            {
                return null;
            }
            /// <inheritdoc />
            public IQueueOutputMessages Send(List<QueueMessage<IMessage, IAdditionalMessageData>> messages)
            {
                return null;
            }

            public async Task<IQueueOutputMessage> SendAsync(IMessage messageToSend, IAdditionalMessageData data)
            {
                return await Task.FromResult<QueueOutputMessage>(null).ConfigureAwait(false);
            }

            public async Task<IQueueOutputMessages> SendAsync(List<QueueMessage<IMessage, IAdditionalMessageData>> messages)
            {
                return await Task.FromResult<QueueOutputMessages>(null).ConfigureAwait(false);
            }
        }
    }
}
