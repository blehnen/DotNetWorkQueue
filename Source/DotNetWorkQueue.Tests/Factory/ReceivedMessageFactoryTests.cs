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
using DotNetWorkQueue.Factory;
using DotNetWorkQueue.Messages;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;
namespace DotNetWorkQueue.Tests.Factory
{
    public class ReceivedMessageFactoryTests
    {
        [Fact]
        public void Create_Message()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            IMessage message = fixture.Create<Message>();
            var messageId = fixture.Create<IMessageId>();
            var correlationId = fixture.Create<ICorrelationId>();

            var factory = Create(fixture);
            var messageinternal = factory.Create(message,
                messageId,
                correlationId);

            Assert.Equal(messageinternal.MesssageId, messageId);
            Assert.Equal(messageinternal.Body, message.Body);
            Assert.Equal(messageinternal.Headers, message.Headers);
            Assert.Equal(messageinternal.CorrelationId, correlationId);
        }
        private IReceivedMessageFactory Create(IFixture fixture)
        {
            return fixture.Create<ReceivedMessageFactory>();
        }
    }
}
