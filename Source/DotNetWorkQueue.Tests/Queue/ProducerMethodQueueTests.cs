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
using DotNetWorkQueue.Messages;
using DotNetWorkQueue.Queue;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Ploeh.AutoFixture.Xunit2;
using Xunit;

namespace DotNetWorkQueue.Tests.Queue
{
    public class ProducerMethodQueueTests
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "part of test")]
        [Fact]
        public void Call_Dispose_Multiple_Times_Ok()
        {
            using (var test = CreateQueue())
            {
                test.Dispose();
            }
        }

        [Fact]
        public void Get_ReadOnlyConfiguration_Set_After_First_Message()
        {
            var test = CreateQueue();
            test.Send((message, notification) => Console.WriteLine("hello"));
            Assert.True(test.Configuration.IsReadOnly);
        }

        [Fact]
        public void Send_Null_AdditionalData_NoException()
        {
            using (var test = CreateQueue())
            {
                test.Send((message, notification) => Console.WriteLine("hello"));
            }
        }

        [Fact]
        public void Send_Message()
        {
            using (var test = CreateQueue())
            {
                test.Send((message, notification) => Console.WriteLine("hello"));
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
                test.Send((message, notification) => Console.WriteLine("hello"), data);
            }
        }

        private ProducerMethodQueue CreateQueue()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            fixture.Inject(CreateBaseQueue(fixture));
            return fixture.Create<ProducerMethodQueue>();
        }
        private IProducerQueue<MessageExpression> CreateBaseQueue(IFixture fixture)
        {
            return fixture.Create<ProducerQueue<MessageExpression>>();
        }
    }
}
