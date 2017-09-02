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
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;

namespace DotNetWorkQueue.Tests.Queue
{
    public class MessageProcessingRpcSendTests
    {
        [Fact]
        public void Run_No_Data()
        {
            var test = Create();
            test.Handle(new FakeMessage(), TimeSpan.FromMilliseconds(1000));
        }

        [Fact]
        public void Run_Data()
        {
            var test = Create();
            test.Handle(new FakeMessage(), new FakeAMessageData(),TimeSpan.FromMilliseconds(1000));
        }

        public
            MessageProcessingRpcSend<FakeMessage> Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());

            var sentMessage = fixture.Create<ISentMessage>();
            fixture.Inject(sentMessage);

            var output = fixture.Create<QueueOutputMessage>();
            var sendMessages = fixture.Create<ISendMessages>();
            sendMessages.Send(fixture.Create<IMessage>(),
                  fixture.Create<IAdditionalMessageData>()).ReturnsForAnyArgs(output);
            fixture.Inject(sendMessages);

            fixture.Inject(fixture.Create<ProducerQueue<FakeMessage>>());
            return fixture.Create<MessageProcessingRpcSend<FakeMessage>>();
        }
    }
}
