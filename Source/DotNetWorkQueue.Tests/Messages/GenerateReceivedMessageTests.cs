using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Messages;


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Messages
{
    [TestClass]
    public class GenerateReceivedMessageTests
    {
        [TestMethod]
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

            Assert.AreEqual(translatedMessage.Body, rec.Body);
            Assert.AreEqual(translatedMessage.CorrelationId, rec.CorrelationId);
            CollectionAssert.AreEquivalent((System.Collections.ICollection)translatedMessage.Headers, (System.Collections.ICollection)rec.Headers);
            Assert.AreEqual(translatedMessage.MessageId, rec.MessageId);
        }

        private class FakeMessage
        {

        }
    }
}
