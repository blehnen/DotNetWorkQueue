using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Factory;



using Xunit;

namespace DotNetWorkQueue.Tests.Factory
{
    public class MessageContextDataFactoryTests
    {
        [Fact]
        public void Create_Null_Name_Fails()
        {
            var factory = Create();
            Assert.Throws<ArgumentNullException>(
            delegate
            {
                factory.Create(null, new Data());
            });
        }
        [Theory, AutoData]
        public void Create_MessageContext(string value, Data data)
        {
            var factory = Create();
            var test = factory.Create(value, data);
            Assert.Equal(test.Name, value);
            Assert.Equal(test.Default, data);
        }

        [Theory, AutoData]
        public void Create_MessageContext_Null_Default(string value)
        {
            var factory = Create();
            var test = factory.Create<Data>(value, null);
            Assert.Null(test.Default);
        }
        private IMessageContextDataFactory Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<MessageContextDataFactory>();
        }
        public class Data
        {

        }
    }
}
