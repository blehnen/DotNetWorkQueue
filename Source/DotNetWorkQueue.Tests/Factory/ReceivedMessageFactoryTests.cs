using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Factory;
using DotNetWorkQueue.Messages;
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
            var messageInternal = factory.Create(message,
                messageId,
                correlationId);

            Assert.Equal(messageInternal.MessageId, messageId);
            Assert.Equal(messageInternal.Body, message.Body);
            Assert.Equal(messageInternal.Headers, message.Headers);
            Assert.Equal(messageInternal.CorrelationId, correlationId);
        }
        private IReceivedMessageFactory Create(IFixture fixture)
        {
            return fixture.Create<ReceivedMessageFactory>();
        }
    }
}
