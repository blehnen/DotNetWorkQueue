using DotNetWorkQueue.Transport.Redis.Basic;
using DotNetWorkQueue.Transport.Redis.Basic.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutoFixture;
using AutoFixture.AutoNSubstitute;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Query
{
    [TestClass]
    public class GetMetaDataQueryTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            var number = fixture.Create<string>();
            var id = new RedisQueueId(number);
            var test = new GetMetaDataQuery(id);
            Assert.AreEqual(id, test.Id);
        }
    }
}
