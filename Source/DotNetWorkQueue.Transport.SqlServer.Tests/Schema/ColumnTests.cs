#region Using

using System;
using DotNetWorkQueue.Transport.SqlServer.Schema;
using KellermanSoftware.CompareNetObjects;
using Tynamix.ObjectFiller;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Schema
{
    [TestClass]
    public class ColumnTests
    {
        [TestMethod]
        public void GetSet_Nullable()
        {
            var test = new Column();
            var c = test.Nullable;
            test.Nullable = !c;
            Assert.AreEqual(!c, test.Nullable);
        }
        [TestMethod]
        public void GetSet_Default()
        {
            var test = new Column();
            var d = new Default("", "");
            test.Default = d;
            Assert.AreEqual(d, test.Default);
        }
        [TestMethod]
        public void GetSet_Identity()
        {
            var test = new Column();
            var d = new Identity(1, 1);
            test.Identity = d;
            Assert.AreEqual(d, test.Identity);
        }
        [TestMethod]
        public void GetSet_Length()
        {
            var test = new Column();
            var c = test.Length;
            test.Length = c + 1;
            Assert.AreEqual(c + 1, test.Length);
        }
        [TestMethod]
        public void GetSet_Position()
        {
            var test = new Column();
            var c = test.Position;
            test.Position = c + 1;
            Assert.AreEqual(c + 1, test.Position);
        }
        [TestMethod]
        public void GetSet_Precision()
        {
            var b = new byte();
            var test = new Column { Precision = b };
            Assert.AreEqual(b, test.Precision);
        }
        [TestMethod]
        public void GetSet_Scale()
        {
            var test = new Column();
            var c = test.Scale;
            test.Scale = c + 1;
            Assert.AreEqual(c + 1, test.Scale);
        }
        [TestMethod]
        public void GetSet_Name()
        {
            var test = new Column { Name = "test" };
            Assert.AreEqual("test", test.Name);
        }
        [TestMethod]
        public void GetSet_Types()
        {
            var test = new Column();
            foreach (ColumnTypes type in Enum.GetValues(typeof(ColumnTypes)))
            {
                test.Type = type;
                Assert.AreEqual(type, test.Type);
            }
        }

        [TestMethod]
        public void Clone()
        {
            var pFiller = new Filler<Column>();
            var test = pFiller.Create();
            var clone = test.Clone(false);
            var compareLogic = new CompareLogic();
            var result = compareLogic.Compare(test, clone);
            Assert.IsTrue(result.AreEqual, result.DifferencesString);
        }

        [TestMethod]
        public void CloneNewName()
        {
            var pFiller = new Filler<Column>();
            var test = pFiller.Create();
            var clone = test.Clone(true);
            var compareLogic = new CompareLogic();
            compareLogic.Config.MembersToIgnore.Add("Name");
            var result = compareLogic.Compare(test, clone);
            Assert.IsTrue(result.AreEqual, result.DifferencesString);
            Assert.AreNotEqual(test.Default.Name, clone.Default.Name);
        }

        [TestMethod]
        public void CloneWithNullDefault1()
        {
            var pFiller = new Filler<Column>();
            var test = pFiller.Create();
            test.Default = null;
            var clone = test.Clone(false);
            var compareLogic = new CompareLogic();
            var result = compareLogic.Compare(test, clone);
            Assert.IsTrue(result.AreEqual, result.DifferencesString);
        }

        [TestMethod]
        public void CloneWithNullDefault2()
        {
            var pFiller = new Filler<Column>();
            var test = pFiller.Create();
            test.Default = null;
            var clone = test.Clone(true);
            var compareLogic = new CompareLogic();
            var result = compareLogic.Compare(test, clone);
            Assert.IsTrue(result.AreEqual, result.DifferencesString);
        }
    }
}
