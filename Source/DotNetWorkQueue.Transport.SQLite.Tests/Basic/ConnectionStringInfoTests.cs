
using DotNetWorkQueue.Transport.SQLite.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic
{
    [TestClass]
    public class ConnectionStringInfoTests
    {
        [TestMethod]
        public void Create_ConnectionStringInfo()
        {
            var test = new ConnectionStringInfo(false, @"c:\test\temp.db3");
            Assert.IsFalse(test.IsInMemory);
            Assert.AreEqual(@"c:\test\temp.db3", test.FileName);
            Assert.IsTrue(test.IsValid);
        }
        [TestMethod]
        public void Create_InMemoryIsValid()
        {
            var test = new ConnectionStringInfo(true, string.Empty);
            Assert.IsTrue(test.IsValid);
        }
    }
}
