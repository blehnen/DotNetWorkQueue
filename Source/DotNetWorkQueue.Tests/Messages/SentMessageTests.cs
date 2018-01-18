using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Messages;


using Xunit;

namespace DotNetWorkQueue.Tests.Messages
{
    public class SentMessageTests
    {
        [Fact]
        public void Get_MessageId()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var messageId = fixture.Create<IMessageId>();
            var id = fixture.Create<ICorrelationId>();
            fixture.Inject(messageId);
            fixture.Inject(id);
            var test = fixture.Create<SentMessage>();
            Assert.Equal(test.MessageId, messageId);
        }

        [Fact]
        public void Get_CorrelationId()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var messageId = fixture.Create<IMessageId>();
            var id = fixture.Create<ICorrelationId>();
            fixture.Inject(messageId);
            fixture.Inject(id);
            var test = fixture.Create<SentMessage>();
            Assert.Equal(test.CorrelationId, id);
        }
    }
}
