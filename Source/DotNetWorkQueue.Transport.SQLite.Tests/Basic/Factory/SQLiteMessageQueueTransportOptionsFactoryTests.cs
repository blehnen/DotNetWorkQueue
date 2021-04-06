using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Transport.SQLite.Basic.Factory;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic.Factory
{
    public class SqLiteMessageQueueTransportOptionsFactoryTests
    {
        [Fact]
        public void Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<SqLiteMessageQueueTransportOptionsFactory>();
            test.Create();
        }
    }
}
