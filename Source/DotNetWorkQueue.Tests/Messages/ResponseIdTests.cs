using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Messages;



using Xunit;

namespace DotNetWorkQueue.Tests.Messages
{
    public class ResponseIdTests
    {
        [Fact]
        public void Get_MessageId()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var messageId = fixture.Create<IMessageId>();
            fixture.Inject(messageId);
            var test = fixture.Create<ResponseId>();
            Assert.Equal(test.MessageId, messageId);
        }

        [Theory, AutoData]
        public void Get_TimeOut(TimeSpan value)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = new ResponseId(fixture.Create<IMessageId>(), value);
            Assert.Equal(test.TimeOut, value);
        }
    }
}
