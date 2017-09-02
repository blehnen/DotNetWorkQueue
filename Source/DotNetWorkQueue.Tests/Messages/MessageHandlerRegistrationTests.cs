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
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;

namespace DotNetWorkQueue.Tests.Messages
{
    public class MessageHandlerRegistrationTests
    {
        [Fact]
        public void Create_Null_Set_Fails()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageHandlerRegistration>();
            Assert.Throws<ArgumentNullException>(
            delegate
            {
                test.Set<FakeMessage>(null);
            });
        }

        [Fact]
        public void GetHandler()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<MessageHandlerRegistration>();

            void Action(IReceivedMessage<FakeMessage> message, IWorkerNotification worker)
            {
            }

            test.Set((Action<IReceivedMessage<FakeMessage>, IWorkerNotification>) Action);
            Assert.Equal((Action<IReceivedMessage<FakeMessage>, IWorkerNotification>) Action, test.GetHandler());
        }

        [Fact]
        public void GenerateMessage()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());

            IGenerateReceivedMessage gen = fixture.Create<GenerateReceivedMessage>();
            var inputMessage = fixture.Create<IMessage>();
            inputMessage.Body.Returns(new FakeMessage());
            fixture.Inject(gen);
            fixture.Inject(inputMessage);
            var test = fixture.Create<MessageHandlerRegistration>();

            void Action(IReceivedMessage<FakeMessage> recMessage, IWorkerNotification worker)
            {
            }

            test.Set((Action<IReceivedMessage<FakeMessage>, IWorkerNotification>) Action);

            IReceivedMessageInternal rec = fixture.Create<ReceivedMessageInternal>();

            var message = test.GenerateMessage(rec);

            Assert.IsAssignableFrom<FakeMessage>(message.Body);
        }

        public class FakeMessage
        {
            
        }
    }
}
