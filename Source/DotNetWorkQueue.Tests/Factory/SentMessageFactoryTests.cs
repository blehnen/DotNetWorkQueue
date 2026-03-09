using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Factory;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Factory
{
    [TestClass]
    public class SentMessageFactoryTests
    {
        [TestMethod]
        public void Create_SentMessage()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var messageId = fixture.Create<IMessageId>();
            var correlationId = fixture.Create<ICorrelationId>();

            var factory = Create(fixture);
            var id = factory.Create(messageId, correlationId);

            Assert.AreEqual(id.MessageId, messageId);
            Assert.AreEqual(id.CorrelationId, correlationId);
        }

        [TestMethod]
        public void Create_With_Null_Fails()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var factory = Create(fixture);
            Assert.ThrowsExactly<ArgumentNullException>(
                delegate
                {
                    factory.Create(null, null);
                });
        }
        private ISentMessageFactory Create(IFixture fixture)
        {
            return fixture.Create<SentMessageFactory>();
        }
    }
}
