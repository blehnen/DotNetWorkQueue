using System;
using System.Collections.Generic;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Messages;
using NSubstitute;



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

            Assert.Equal(messageInternal.MessageId, messageId);
            Assert.Equal(messageInternal.Body, message.Body);
            Assert.Equal(messageInternal.CorrelationId, correlationId);
            Assert.Equal(messageInternal.Headers, message.Headers);
        }

        private class FakeMessage
        {
        }
    }
}
