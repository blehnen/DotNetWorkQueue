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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Queue;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
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
            return new RpcQueue<FakeMessage, FakeMessage>(fixture.Create<QueueRpcConfiguration>(),
                fixture.Create<QueueConsumerConfiguration>(),
                fixture.Create<IClearExpiredMessagesRpcMonitor>(),
                fixture.Create<ILogFactory>(),
                fixture.Create<MessageProcessingRpcReceive<FakeMessage>>(),
                fixture.Create<MessageProcessingRpcSend<FakeMessage>>(),
                fixture.Create<IQueueWaitFactory>());
        }
    }
}
