using System;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using AutoFixture.Xunit2;
using DotNetWorkQueue.Factory;



using Xunit;

namespace DotNetWorkQueue.Tests.Factory
{
    public class RpcTimeoutFactoryTests
    {
        [Theory, AutoData]
        public void Create_Default(TimeSpan value)
        {
            var factory = Create();
            var info = factory.Create(value);
            Assert.Equal(value, info.Timeout);
        }
        private IRpcTimeoutFactory Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<RpcTimeoutFactory>();
        }
    }
}
