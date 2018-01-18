using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Transport.SqlServer.Basic.Factory;
using Xunit;

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Basic.Factory
{
    public class SqlServerMessageQueueTransportOptionsFactoryTests
    {
        [Fact]
        public void Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<SqlServerMessageQueueTransportOptionsFactory>();
            test.Create();
        }
    }
}
