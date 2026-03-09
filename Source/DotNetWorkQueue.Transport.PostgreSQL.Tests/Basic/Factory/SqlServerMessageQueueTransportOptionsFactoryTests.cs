using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Transport.PostgreSQL.Basic.Factory;


using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Basic.Factory
{
    [TestClass]
    public class PostgreSqlMessageQueueTransportOptionsFactoryTests
    {
        [TestMethod]
        public void Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<PostgreSqlMessageQueueTransportOptionsFactory>();
            test.Create();
        }
    }
}
