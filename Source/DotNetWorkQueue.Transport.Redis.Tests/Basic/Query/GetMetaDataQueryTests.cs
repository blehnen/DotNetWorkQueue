using AutoFixture.Xunit2;
using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.Basic.Query;
using Xunit;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Query
{
    public class GetMetaDataQueryTests
    {
        [Theory, AutoData]
        public void Create_Default(string number)
        {
            var id = new RedisQueueId(number);
            var test = new GetMetaDataQuery(id);
            Assert.Equal(id, test.Id);
        }
    }
}
