#region Using

using DotNetWorkQueue.Transport.SQLite.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace DotNetWorkQueue.Transport.SQLite.Tests.Schema
{
    [TestClass]
    public class DefaultTests
    {
        [TestMethod]
        public void Default()
        {
            var test = new Default("test", "test1");
            Assert.AreEqual("test", test.Name);
            Assert.AreEqual("test1", test.Value);
        }
        [TestMethod]
        public void Script()
        {
            var test = new Default("test", "test1");
            StringAssert.Contains(test.Script(), "test");
            StringAssert.Contains(test.Script(), "test1");
        }
    }
}
