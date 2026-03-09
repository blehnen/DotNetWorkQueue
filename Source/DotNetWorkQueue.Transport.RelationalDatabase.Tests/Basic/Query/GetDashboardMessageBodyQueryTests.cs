using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Query
{
    [TestClass]
    public class GetDashboardMessageBodyQueryTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var test = new GetDashboardMessageBodyQuery("42");
            Assert.AreEqual("42", test.MessageId);
        }
    }
}
