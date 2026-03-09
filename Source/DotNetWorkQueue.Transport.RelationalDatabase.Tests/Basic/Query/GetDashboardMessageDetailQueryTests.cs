using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Query
{
    [TestClass]
    public class GetDashboardMessageDetailQueryTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var test = new GetDashboardMessageDetailQuery("42");
            Assert.AreEqual("42", test.MessageId);
        }
    }
}
