using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Messages;


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Messages
{
    [TestClass]
    public class SentMessageTests
    {
        [TestMethod]
        public void Get_MessageId()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var messageId = fixture.Create<IMessageId>();
            var id = fixture.Create<ICorrelationId>();
            fixture.Inject(messageId);
            fixture.Inject(id);
            var test = fixture.Create<SentMessage>();
            Assert.AreEqual(test.MessageId, messageId);
        }

        [TestMethod]
        public void Get_CorrelationId()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var messageId = fixture.Create<IMessageId>();
            var id = fixture.Create<ICorrelationId>();
            fixture.Inject(messageId);
            fixture.Inject(id);
            var test = fixture.Create<SentMessage>();
            Assert.AreEqual(test.CorrelationId, id);
        }
    }
}
