// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
using System.Diagnostics.CodeAnalysis;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Serialization;
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;

namespace DotNetWorkQueue.Tests.Queue
{
    public class RpcMethodQueueTests
    {
        [Fact]
        public void IsDisposed_False_By_Default()
        {
            using (var test = Create())
            {
                Assert.Equal(test.IsDisposed, false);
            }
        }

        [Fact]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            var test = Create();
            test.Dispose();
            Assert.Equal(test.IsDisposed, true);
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
                test.Send((message, notification) => 1+1, TimeSpan.FromDays(1));
            }
        }

        [Fact]
        public void Send_Message_With_Data()
        {
            using (var test = Create())
            {
                test.Start();
                test.Send((message, notification) => 1 + 1, TimeSpan.FromDays(1), new FakeAMessageData());
            }
        }
        [Fact]
        public void Send_Data_Null_NoException()
        {
            using (var test = Create())
            {
                test.Start();
                test.Send((message, notification) => 1 + 1, TimeSpan.FromDays(1));
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
                test.Send((message, notification) => 1 + 1, TimeSpan.FromDays(1));
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
                test.Send((message, notification) => 1 + 1, TimeSpan.FromDays(1), new FakeAMessageData());
            });
            }
        }

        private RpcMethodQueue Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            fixture.Inject(CreateSerializer(fixture));
            fixture.Inject(CreateRpc(fixture));
            return fixture.Create<RpcMethodQueue>();
        }

        private IExpressionSerializer CreateSerializer(IFixture fixture)
        {
            return fixture.Create<JsonExpressionSerializer>();
        }
        private IRpcQueue<object, MessageExpression> CreateRpc(IFixture fixture)
        {
            fixture.Inject(new MessageProcessingRpcReceiveTests().Create());
            var receive = fixture.Create<IMessageProcessingRpcSend<MessageExpression>>();
            receive
                .Handle(null, null, TimeSpan.MaxValue)
                .ReturnsForAnyArgs(new SentMessage(new RpcQueueTests.MessageId(), new RpcQueueTests.CorrelationId()));
            receive
               .Handle(null, TimeSpan.MaxValue)
               .ReturnsForAnyArgs(new SentMessage(new RpcQueueTests.MessageId(), new RpcQueueTests.CorrelationId()));
            fixture.Inject(receive);

            var message = fixture.Create<IMessage>();
            fixture.Inject(message);

            var newMessage = new ReceivedMessage<object>(new ReceivedMessageInternal(message,
                new RpcQueueTests.MessageId(), new RpcQueueTests.CorrelationId()));

            var handler = fixture.Create<IMessageHandlerRegistration>();
            handler.GenerateMessage(null).ReturnsForAnyArgs(newMessage);
            fixture.Inject(handler);

            return new RpcQueue<object, MessageExpression>(fixture.Create<QueueRpcConfiguration>(),
                fixture.Create<QueueConsumerConfiguration>(),
                fixture.Create<IClearExpiredMessagesRpcMonitor>(),
                fixture.Create<ILogFactory>(),
                fixture.Create<MessageProcessingRpcReceive<object>>(),
                receive,
                fixture.Create<IQueueWaitFactory>());
        }
    }
}
