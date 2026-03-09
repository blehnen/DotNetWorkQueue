#region Using

using System;
using DotNetWorkQueue.Transport.PostgreSQL.Schema;
using KellermanSoftware.CompareNetObjects;
using Tynamix.ObjectFiller;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Schema
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
        public void GetSet_Identity()
        {
            var test = new Column { Identity = true };
            Assert.IsTrue(test.Identity);
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
            var clone = test.Clone();
            var compareLogic = new CompareLogic();
            var result = compareLogic.Compare(test, clone);
            Assert.IsTrue(result.AreEqual, result.DifferencesString);
        }
    }
}
