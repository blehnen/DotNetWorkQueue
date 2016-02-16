// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using DotNetWorkQueue.Queue;
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;
namespace DotNetWorkQueue.Tests.Queue
{
    public class MessageProcessingAsyncTests
    {
        [Fact]
        public void Handle()
        {
            var wrapper = new MessageProcessingAsyncWrapper();
            var test = wrapper.Create();
            test.Handle();
            wrapper.MessageContextFactory.Received(1).Create();
        }

        [Fact]
        public void Handle_Receive_Message()
        {
            var wrapper = new MessageProcessingAsyncWrapper();
            var test = wrapper.Create();
            test.Handle();
            wrapper.ReceiveMessages.ReceivedWithAnyArgs(1).Create();
        }

        public class MessageProcessingAsyncWrapper
        {
            private readonly IFixture _fixture;
            public IReceiveMessagesFactory ReceiveMessages;
            public IMessageContextFactory MessageContextFactory;
            public MessageProcessingAsyncWrapper()
            {
                _fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
                ReceiveMessages = _fixture.Create<IReceiveMessagesFactory>();
                MessageContextFactory = _fixture.Create<IMessageContextFactory>();
                _fixture.Inject(ReceiveMessages);
                _fixture.Inject(MessageContextFactory);
            }
            public MessageProcessingAsync Create()
            {
                return _fixture.Create<MessageProcessingAsync>();
            }
        }
    }
}
