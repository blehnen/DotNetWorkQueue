using AutoFixture;
using AutoFixture.AutoNSubstitute;
using DotNetWorkQueue.Queue;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Queue
{
    [TestClass]
    public class QueryMessageHistoryNoOpTests
    {
        [TestMethod]
        public void Get_Returns_Empty_List()
        {
            var test = Create();
            var result = test.Get(0, 10, null);
            Assert.AreEqual(0, result.Count);
        }

        [TestMethod]
        public void GetByQueueId_Returns_Null()
        {
            var test = Create();
            var result = test.GetByQueueId("1");
            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetCount_Returns_Zero()
        {
            var test = Create();
            var result = test.GetCount(null);
            Assert.AreEqual(0, result);
        }

        private IQueryMessageHistory Create()
        {
            var fixture = new Fixture().Customize(new AutoNSubstituteCustomization());
            return fixture.Create<QueryMessageHistoryNoOp>();
        }
    }
}
