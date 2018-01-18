using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Factory;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Factory
{
    public class ReceiveMessagesFactoryTests
    {
        [Fact]
        public void Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<ReceiveMessagesFactory>();
            test.Create();
        }
    }
}
