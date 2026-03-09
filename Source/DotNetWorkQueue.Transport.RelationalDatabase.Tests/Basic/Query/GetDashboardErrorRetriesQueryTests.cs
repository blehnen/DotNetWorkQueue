using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Query
{
    [TestClass]
    public class GetDashboardErrorRetriesQueryTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var test = new GetDashboardErrorRetriesQuery("99");
            Assert.AreEqual("99", test.MessageId);
        }
    }
}
