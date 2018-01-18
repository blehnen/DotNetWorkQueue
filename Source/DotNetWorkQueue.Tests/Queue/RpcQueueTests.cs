using System;
using System.Diagnostics.CodeAnalysis;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Queue;
using NSubstitute;


using Xunit;

// ReSharper disable AccessToDisposedClosure
namespace DotNetWorkQueue.Tests.Queue
{
    public class RpcQueueTests
    {
        [Fact]
        public void IsDisposed_False_By_Default()
        {
            using (var test = Create())
            {
                Assert.False(test.IsDisposed);
            }
        }

        [Fact]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            var test = Create();
            test.Dispose();
            Assert.True(test.IsDisposed);
        }

        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [Fact]
        public void Call_Dispose_Multiple_Times_Ok()
        {
            using (var test = Create())
            {
                test.Dispose();
            }
        }

        [Fact]
        public void Disposed_Instance_Get_Configuration_Exception()
        {
            var test = Create();
            test.Dispose();
            Assert.Throws<ObjectDisposedException>(
                delegate
                {
                    test.Configuration.MessageExpiration.Enabled = true;
                });
        }

        [Fact]
        public void Disposed_Instance_Start_Exception()
        {
            var test = Create();
            test.Dispose();
            Assert.Throws<ObjectDisposedException>(
                delegate
                {
                    test.Start();
                });
        }

        [Fact]
        public void Calling_Start_Multiple_Times_Exception()
        {
            using (var test = Create())
            {
                test.Start();
                Assert.Throws<DotNetWorkQueueException>(
                    delegate
                    {
                        test.Start();
                    });
            }
        }

        [Fact]
        public void Send_Message_NoData()
        {
            using (var test = Create())
            {
                test.Start();
                test.Send(new FakeMessage(), TimeSpan.FromDays(1));
            }
        }

        [Fact]
        public void Send_Message_With_Data()
        {
           using (var test = Create())
            {
                test.Start();
                test.Send(new FakeMessage(), TimeSpan.FromDays(1), new FakeAMessageData());
            }
        }

        [Fact]
        public void Send_Message_Null_Exception()
        {
            using (var test = Create())
            {
                test.Start();
                Assert.Throws<ArgumentNullException>(
            delegate
            {
                test.Send(null, TimeSpan.FromDays(1));
            });
            }
        }

        [Fact]
        public void Send_Data_Null_NoException()
        {
            using (var test = Create())
            {
                test.Start();
                test.Send(new FakeMessage(), TimeSpan.FromDays(1));
            }
        }

        [Fact]
        public void Send_Message_Disposed_Exception()
        {
            using (var test = Create())
            {
                test.Start();
                test.Dispose();
                Assert.Throws<ObjectDisposedException>(
            delegate
            {
                test.Send(null, TimeSpan.FromDays(1));
            });
            }
        }

        [Fact]
        public void Send_Data_Disposed_Exception()
        {
            using (var test = Create())
            {
                test.Start();
                test.Dispose();
                Assert.Throws<ObjectDisposedException>(
            delegate
            {
                test.Send(new FakeMessage(), TimeSpan.FromDays(1), new FakeAMessageData());
            });
            }
        }

        private RpcQueue<FakeMessage, FakeMessage> Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            fixture.Inject(new MessageProcessingRpcReceiveTests().Create());

            var receive = fixture.Create<IMessageProcessingRpcSend<FakeMessage>>();
            receive
                .Handle(null, null, TimeSpan.MaxValue)
                .ReturnsForAnyArgs(new SentMessage(new MessageId(), new CorrelationId()));
            receive
               .Handle(null, TimeSpan.MaxValue)
               .ReturnsForAnyArgs(new SentMessage(new MessageId(), new CorrelationId()));
            fixture.Inject(receive);

            return new RpcQueue<FakeMessage, FakeMessage>(fixture.Create<QueueRpcConfiguration>(),
                fixture.Create<QueueConsumerConfiguration>(),
                fixture.Create<IClearExpiredMessagesRpcMonitor>(),
                fixture.Create<ILogFactory>(),
                fixture.Create<MessageProcessingRpcReceive<FakeMessage>>(),
                receive,
                fixture.Create<IQueueWaitFactory>());
        }

        public class MessageId : IMessageId
        {
            public bool HasValue => true;

            public ISetting Id => new Setting<int>(1);
        }

        public class CorrelationId : ICorrelationId
        {
            public bool HasValue => true;

            public ISetting Id
            {
                get
                {
                    return new Setting<Guid>(Guid.NewGuid());
                }

                set
                {
                   
                }
            }
        }
    }
}
