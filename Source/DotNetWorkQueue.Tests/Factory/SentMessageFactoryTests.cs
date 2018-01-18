using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Factory;
using Xunit;

namespace DotNetWorkQueue.Tests.Factory
{
    public class SentMessageFactoryTests
    {
        [Fact]
        public void Create_SentMessage()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var messageId = fixture.Create<IMessageId>();
            var correlationId = fixture.Create<ICorrelationId>();

            var factory = Create(fixture);
            var id = factory.Create(messageId, correlationId);

            Assert.Equal(id.MessageId, messageId);
            Assert.Equal(id.CorrelationId, correlationId);
        }

        [Fact]
        public void Create_With_Null_Fails()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var factory = Create(fixture);
            Assert.Throws<ArgumentNullException>(
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
