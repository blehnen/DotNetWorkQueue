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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using DotNetWorkQueue.Messages;
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Ploeh.AutoFixture.Xunit2;
using Xunit;
namespace DotNetWorkQueue.Tests.Messages
{
    public class ReceivedMessageTests
    {
        [Fact]
        public void Create_MessageId_Equals()
        {
            var message = CreateMessage();
            var test = new ReceivedMessage<FakeMessage>(message);
            Assert.Equal(test.MessageId, message.MesssageId);
        }

        [Fact]
        public void Create_Body_Equals()
        {
            var message = CreateMessage();
            var test = new ReceivedMessage<FakeMessage>(message);
            Assert.Equal(test.Body, message.Body);
        }

        [Fact]
        public void Create_CorrelationId_Equals()
        {
            var message = CreateMessage();
            var test = new ReceivedMessage<FakeMessage>(message);
            Assert.Equal(test.CorrelationId, message.CorrelationId);
        }

        [Theory, AutoData]
        public void Create_Headers_Equals(string value)
        {
            var message = CreateMessage();

            var headers = new Dictionary<string, object> {{ value, new UriBuilder()}};

            message.Headers.Returns(new ReadOnlyDictionary<string, object>(headers));
            var test = new ReceivedMessage<FakeMessage>(message);
            Assert.Equal(test.Headers, message.Headers);
        }

        [Theory, AutoData]
        public void GetHeader(string value)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var message = CreateMessage();
            var test = new ReceivedMessage<FakeMessage>(message);

            var messageContextDataFactory = fixture.Create<IMessageContextDataFactory>();

            var headerData = new HeaderData();
            messageContextDataFactory.Create(value, headerData)
                .Returns(new MessageContextData<HeaderData>(value, headerData));

            var property = messageContextDataFactory.Create(value, headerData);

            Assert.Equal(test.GetHeader(property), headerData);
        }

        private IReceivedMessageInternal CreateMessage()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var message = fixture.Create<IReceivedMessageInternal>();
            message.Body.Returns(new FakeMessage());
            return message;
        }

        private class FakeMessage
        {
            
        }
        public class HeaderData 
        {
        }
    }
}
