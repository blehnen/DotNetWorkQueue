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

using DotNetWorkQueue.Messages;
using Ploeh.AutoFixture;
using Ploeh.AutoFixture.AutoNSubstitute;
using Xunit;
namespace DotNetWorkQueue.Tests.Messages
{
    public class GenerateReceivedMessageTests
    {
        [Fact]
        public void TestCreation()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());

            IGenerateReceivedMessage gen = fixture.Create<GenerateReceivedMessage>();
            var inputMessage = fixture.Create<IMessage>();
            inputMessage.Body.Returns(new FakeMessage());
            fixture.Inject(inputMessage);
            IReceivedMessageInternal rec = fixture.Create<ReceivedMessageInternal>();

            dynamic message = gen.GenerateMessage(typeof (FakeMessage), rec);

            IReceivedMessage<FakeMessage> translatedMessage = message;

            Assert.Equal(translatedMessage.Body, rec.Body);
            Assert.Equal(translatedMessage.CorrelationId, rec.CorrelationId);
            Assert.Equal(translatedMessage.Headers, rec.Headers);
            Assert.Equal(translatedMessage.MessageId, rec.MesssageId);
        }

        private class FakeMessage
        {
            
        }
    }
}
