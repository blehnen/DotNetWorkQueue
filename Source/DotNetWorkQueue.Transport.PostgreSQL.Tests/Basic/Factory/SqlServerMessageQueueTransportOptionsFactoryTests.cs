using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Transport.PostgreSQL.Basic.Factory;


using Xunit;

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Basic.Factory
{
    public class PostgreSqlMessageQueueTransportOptionsFactoryTests
    {
        [Fact]
        public void Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<PostgreSqlMessageQueueTransportOptionsFactory>();
            test.Create();
        }
    }
}
