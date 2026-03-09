#region Using

using DotNetWorkQueue.Transport.SqlServer.Schema;
using KellermanSoftware.CompareNetObjects;
using Tynamix.ObjectFiller;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Schema
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

        [TestMethod]
        public void Clone()
        {
            var pFiller = new Filler<Default>();
            var test = pFiller.Create();
            var clone = test.Clone();
            var compareLogic = new CompareLogic();
            var result = compareLogic.Compare(test, clone);
            Assert.IsTrue(result.AreEqual, result.DifferencesString);
        }

        [TestMethod]
        public void CloneNewName()
        {
            var pFiller = new Filler<Default>();
            var test = pFiller.Create();
            var clone = test.Clone("ANewName");
            var compareLogic = new CompareLogic();
            compareLogic.Config.MembersToIgnore.Add("Name");
            var result = compareLogic.Compare(test, clone);
            Assert.IsTrue(result.AreEqual, result.DifferencesString);
            Assert.AreNotEqual(test.Name, clone.Name);
        }
    }
}
