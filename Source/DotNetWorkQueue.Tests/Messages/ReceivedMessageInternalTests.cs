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
using System;
using System.Collections.Generic;
using DotNetWorkQueue.Messages;
using NSubstitute;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Ploeh.AutoFixture.Xunit2;
using Xunit;
namespace DotNetWorkQueue.Tests.Messages
{
    public class ReceivedMessageInternalTests
    {
        [Theory, AutoData]
        public void Create_Properties_Equals(string value)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var message = fixture.Create<IMessage>();

            message.Body.Returns(new FakeMessage());
            message.Headers.Returns(new Dictionary<string, object>());

            message.Headers.Add(value, new UriBuilder());

            var messageId = fixture.Create<IMessageId>();
            var correlationId = fixture.Create<ICorrelationId>();
            fixture.Inject(message);
            fixture.Inject(messageId);
            fixture.Inject(correlationId);
            var messageInternal = fixture.Create<ReceivedMessageInternal>();

            Assert.Equal(messageInternal.MesssageId, messageId);
            Assert.Equal(messageInternal.Body, message.Body);
            Assert.Equal(messageInternal.CorrelationId, correlationId);
            Assert.Equal(messageInternal.Headers, message.Headers);
        }

        private class FakeMessage
        {
        }
    }
}
