using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.Shared.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Tests.Basic
{
    public class SqlHeadersTests
    {
        [Fact]
        public void Create_Default()
        {
            var test = Create();
            Assert.NotNull(test.QueueDelay);
        }

        private IIncreaseQueueDelay Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<IncreaseQueueDelay>();
        }
    }
}
