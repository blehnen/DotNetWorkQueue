using System.Linq;
using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Transport.Redis.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic
{
    public class RedisQueueStatusProviderTests
    {
        [Fact]
        public void GetData()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var test = fixture.Create<RedisQueueStatusProvider>();
            Assert.Equal(4, test.Current.Data.Count());
        }
    }
}
