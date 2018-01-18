using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Factory;
using Xunit;

namespace DotNetWorkQueue.Tests.Factory
{
    public class RpcContextFactoryTests
    {
        [Theory, AutoData]
        public void Create_Default(TimeSpan value)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var messageId = fixture.Create<IMessageId>();

            var factory = Create(fixture);
            var info = factory.Create(messageId, value);

            Assert.Equal(info.MessageId, messageId);
            Assert.Equal(info.Timeout, value);
        }
        [Fact]
        public void Create_Default_TimeSpan_Null()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var messageId = fixture.Create<IMessageId>();

            var factory = Create(fixture);
            var info = factory.Create(messageId, null);

            Assert.Equal(info.MessageId, messageId);
            Assert.Null(info.Timeout);
        }
        [Fact]
        public void Create_Null_Params_Fails()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var factory = Create(fixture);
            Assert.Throws<ArgumentNullException>(
               delegate
               {
                   factory.Create(null, null);
               });
        }
        private IRpcContextFactory Create(IFixture fixture)
        {
            return fixture.Create<RpcContextFactory>();
        }
    }
}
