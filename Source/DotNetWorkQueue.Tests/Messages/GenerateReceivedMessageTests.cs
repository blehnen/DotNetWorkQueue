using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Messages;


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

            var message = gen.GenerateMessage(typeof(FakeMessage), rec);

            IReceivedMessage<FakeMessage> translatedMessage = message;

            Assert.Equal(translatedMessage.Body, rec.Body);
            Assert.Equal(translatedMessage.CorrelationId, rec.CorrelationId);
            Assert.Equal(translatedMessage.Headers, rec.Headers);
            Assert.Equal(translatedMessage.MessageId, rec.MessageId);
        }

        private class FakeMessage
        {

        }
    }
}
