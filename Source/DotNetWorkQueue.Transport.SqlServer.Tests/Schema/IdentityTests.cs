#region Using

using DotNetWorkQueue.Transport.SqlServer.Schema;
using KellermanSoftware.CompareNetObjects;
using Tynamix.ObjectFiller;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Schema
{
    [TestClass]
    public class IdentityTests
    {
        [TestMethod]
        public void Default()
        {
            var test = new Identity(1, 2);
            Assert.AreEqual(1, test.Seed);
            Assert.AreEqual(2, test.Increment);
        }
        [TestMethod]
        public void Script()
        {
            var test = new Identity(1, 2);
            StringAssert.Contains(test.Script(), "1");
            StringAssert.Contains(test.Script(), "2");
        }
        [TestMethod]
        public void Clone()
        {
            var pFiller = new Filler<Identity>();
            var test = pFiller.Create();
            var clone = test.Clone();
            var compareLogic = new CompareLogic();
            var result = compareLogic.Compare(test, clone);
            Assert.IsTrue(result.AreEqual, result.DifferencesString);
        }
    }
}
