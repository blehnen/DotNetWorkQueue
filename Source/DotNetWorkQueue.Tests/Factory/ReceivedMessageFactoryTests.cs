using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Factory;
using DotNetWorkQueue.Messages;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Factory
{
    [TestClass]
    public class ReceivedMessageFactoryTests
    {
        [TestMethod]
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

            Assert.AreEqual(messageInternal.MessageId, messageId);
            Assert.AreEqual(messageInternal.Body, message.Body);
            CollectionAssert.AreEquivalent((System.Collections.ICollection)messageInternal.Headers, (System.Collections.ICollection)message.Headers);
            Assert.AreEqual(messageInternal.CorrelationId, correlationId);
        }
        private IReceivedMessageFactory Create(IFixture fixture)
        {
            return fixture.Create<ReceivedMessageFactory>();
        }
    }
}
