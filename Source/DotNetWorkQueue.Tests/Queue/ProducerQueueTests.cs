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
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Queue;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Ploeh.AutoFixture.Xunit2;
using Xunit;
// ReSharper disable AccessToDisposedClosure
namespace DotNetWorkQueue.Tests.Queue
{
    public class ProducerQueueTests
    {
        [Fact]
        public void IsDisposed_False_By_Default()
        {
            using (var test = CreateQueue())
            {
                Assert.Equal(test.IsDisposed, false);
            }
        }

        [Fact]
        public void Disposed_Instance_Sets_IsDisposed()
        {
            var test = CreateQueue();
            test.Dispose();
            Assert.Equal(test.IsDisposed, true);
        }

        [Fact]
        public void Call_Dispose_Multiple_Times_Ok()
        {
            using (var test = CreateQueue())
            {
                test.Dispose();
            }
        }

        [Fact]
        public void Disposed_Instance_Get_Configuration_Exception()
        {
            var test = CreateQueue();
            test.Dispose();
            Assert.Throws<ObjectDisposedException>(
                delegate
                {
                    test.Configuration.TransportConfiguration.ConnectionInfo.QueueName = "shouldnotwork";
                });
        }

        [Fact]
        public void Get_ReadOnlyConfiguration_Set_After_First_Message()
        {
            var test = CreateQueue();
            test.Send(new FakeMessage());
            Assert.True(test.Configuration.IsReadOnly);
        }

        [Fact]
        public void Send_Null_Message_Exception()
        {
            using (var test = CreateQueue())
            {
                Assert.Throws<ArgumentNullException>(
                    delegate
                    {
                        FakeMessage message = null;
                        // ReSharper disable once ExpressionIsAlwaysNull
                        test.Send(message);
                    });
            }
        }

        [Fact]
        public void Send_Null_AdditionalData_NoException()
        {
            using (var test = CreateQueue())
            {
                test.Send(new FakeMessage());
            }
        }

        [Fact]
        public void Send_Message()
        {
            using (var test = CreateQueue())
            {
                test.Send(new FakeMessage());
            }
        }

        [Theory, AutoData]
        public void Send_Message_And_Data(string value)
        {
            using (var test = CreateQueue())
            {
                var data = new FakeAMessageData();
                IMessageContextData<string> header = new MessageContextData<string>(value, string.Empty);
                data.SetHeader(header, value);
                test.Send(new FakeMessage(), data);
            }
        }

        private ProducerQueue
                    <FakeMessage> CreateQueue()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<ProducerQueue<FakeMessage>>();
        }

        public class FakeMessage
        {

        }
    }
}
