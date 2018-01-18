using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Messages;



using Xunit;

namespace DotNetWorkQueue.Tests.Messages
{
    public class RpcContextTests
    {
        [Fact]
        public void Create_Constructor_Null_Timeout()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = new RpcContext(fixture.Create<IMessageId>(), null);
            Assert.NotNull(test);
        }

        [Fact]
        public void Get_MessageId()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var messageId = fixture.Create<IMessageId>();
            var test = new RpcContext(messageId, TimeSpan.Zero);
            Assert.Equal(test.MessageId, messageId);
        }

        [Theory, AutoData]
        public void Get_Timeout(TimeSpan value)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = new RpcContext(fixture.Create<IMessageId>(), value);
            Assert.Equal(test.Timeout, value);
        }
    }
}
