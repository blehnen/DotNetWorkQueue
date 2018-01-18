using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Factory;



using Xunit;

namespace DotNetWorkQueue.Tests.Factory
{
    public class ResponseIdFactoryTests
    {
        [Theory, AutoData]
        public void Create_id(TimeSpan value)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var messageId = fixture.Create<IMessageId>();
            var factory = Create(fixture);
            var id = factory.Create(messageId, value);

            Assert.Equal(id.MessageId, messageId);
            Assert.Equal(id.TimeOut, value);
        }

        [Theory, AutoData]
        public void Create_id_With_Null_MessageID_Fails(TimeSpan value)
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var factory = Create(fixture);
            Assert.Throws<ArgumentNullException>(
                delegate
                {
                    factory.Create(null, value);
                });
        }

        private IResponseIdFactory Create(IFixture fixture)
        {
            return fixture.Create<ResponseIdFactory>();
        }
    }
}
