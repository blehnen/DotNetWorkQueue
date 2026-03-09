using System;
using DotNetWorkQueue.Transport.Shared.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic
{
    [TestClass]
    public class CorrelationIdTests
    {
        [TestMethod]
        public void Create_Default()
        {
            var id = Guid.NewGuid();
            var test = new MessageCorrelationId<Guid>(id);
            Assert.AreEqual(id, test.Id.Value);
            Assert.IsTrue(test.HasValue);
        }
        [TestMethod]
        public void Create_Default_ToString()
        {
            var id = Guid.NewGuid();
            var test = new MessageCorrelationId<Guid>(id);
            Assert.AreEqual(id.ToString(), test.ToString());
        }
        [TestMethod]
        public void Create_Default_Empty_Guid()
        {
            var id = Guid.Empty;
            var test = new MessageCorrelationId<Guid>(id);
            Assert.AreEqual(id, test.Id.Value);
            Assert.IsFalse(test.HasValue);
        }
    }
}
