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
    public class MessageProcessingRpcReceiveTests
    {
        [Fact]
        public void Create_Handle_Fails()
        {
            var test = Create();
            Assert.Throws<ArgumentNullException>(
            delegate
            {
                test.Handle(null, TimeSpan.MinValue, null);
            });
        }

        [Fact]
        public void Create_Handle_Fails2()
        {
            var test = Create();
            Assert.Throws<ArgumentNullException>(
            delegate
            {
                test.Handle(Substitute.For<IMessageId>(), TimeSpan.MinValue, null);
            });
        }

        [Fact]
        public void Create_Default()
        {
            var test = Create();
            test.Handle(Substitute.For<IMessageId>(), TimeSpan.FromMilliseconds(1000),
                Substitute.For<IQueueWait>());
        }

        public MessageProcessingRpcReceive<FakeMessage> Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            IReceiveMessagesFactory receiveMessages = fixture.Create<IReceiveMessagesFactory>();
            fixture.Inject(receiveMessages);
            var fakeMessage = fixture.Create<IMessage>();
            fakeMessage.Body.Returns(new FakeMessage());
            fixture.Inject(fakeMessage);
            ICommitMessage commitMessage = fixture.Create<CommitMessage>();
            fixture.Inject(commitMessage);

            IReceivedMessageInternal message = fixture.Create<ReceivedMessageInternal>();

            IMessageHandlerRegistration messageHandlerRegistration = fixture.Create<IMessageHandlerRegistration>();
            messageHandlerRegistration.GenerateMessage(message)
                   .Returns(new ReceivedMessage<FakeMessage>(message));
            fixture.Inject(messageHandlerRegistration);

            receiveMessages.Create().ReceiveMessage(null).ReturnsForAnyArgs(message);

            return fixture.Create<MessageProcessingRpcReceive<FakeMessage>>();
        }
    }
}
