using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Query
{
    [TestClass]
    public class GetDashboardMessageHeadersQueryTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var test = new GetDashboardMessageHeadersQuery("42");
            Assert.AreEqual("42", test.MessageId);
        }
    }
}
