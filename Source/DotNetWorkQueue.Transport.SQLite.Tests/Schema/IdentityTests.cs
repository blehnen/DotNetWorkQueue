#region Using

using DotNetWorkQueue.Transport.SQLite.Schema;
using KellermanSoftware.CompareNetObjects;
using Tynamix.ObjectFiller;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace DotNetWorkQueue.Transport.SQLite.Tests.Schema
{
    [TestClass]
    public class IdentityTests
    {
        [TestMethod]
        public void Script()
        {
            var test = new Identity();
            StringAssert.Contains(test.Script(), "PRIMARY KEY AUTOINCREMENT");
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
