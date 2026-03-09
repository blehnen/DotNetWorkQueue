using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Query
{
    [TestClass]
    public class GetUtcDateQueryTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var test = new GetUtcDateQuery("test");
            Assert.AreEqual("test", test.ConnectionString);
        }
    }
}
