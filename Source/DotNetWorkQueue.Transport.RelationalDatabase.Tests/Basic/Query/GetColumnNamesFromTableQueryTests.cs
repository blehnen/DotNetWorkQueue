using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Query
{
    [TestClass]
    public class GetColumnNamesFromTableQueryTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var test = new GetColumnNamesFromTableQuery("test1", "test2");
            Assert.AreEqual("test1", test.ConnectionString);
            Assert.AreEqual("test2", test.TableName);
        }
    }
}
