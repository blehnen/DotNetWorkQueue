#region Using

using System.Collections.Generic;
using DotNetWorkQueue.Transport.PostgreSQL.Schema;
using KellermanSoftware.CompareNetObjects;
using Tynamix.ObjectFiller;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Schema
{
    [TestClass]
    public class ConstraintTests
    {
        [TestMethod]
        public void GetSet_Unique()
        {
            var test = new Constraint("test", ConstraintType.Constraint, new List<string>());
            var c = test.Unique;
            test.Unique = !c;
            Assert.AreEqual(!c, test.Unique);
        }
        [TestMethod]
        public void GetSet_Columns()
        {
            var columns = new List<string> { "test" };
            var test = new Constraint("test", ConstraintType.Constraint, columns);
            Assert.AreEqual(columns, test.Columns);
        }
        [TestMethod]
        public void GetSet_Name()
        {
            var test = new Constraint("test", ConstraintType.Constraint, new List<string>()) { Name = "test1" };
            Assert.AreEqual("test1", test.Name);
        }
        [TestMethod]
        public void GetSet_ConstraintType()
        {
            var test = new Constraint("test", ConstraintType.Constraint, new List<string>());
            Assert.AreEqual(ConstraintType.Constraint, test.Type);
        }

        [TestMethod]
        public void Script()
        {
            var test = new Constraint("test", ConstraintType.PrimaryKey, new List<string>());
            StringAssert.Contains(test.Script(), "test");
        }

        [TestMethod]
        public void Clone()
        {
            var pFiller = new Filler<Constraint>();
            var test = pFiller.Create();
            var clone = test.Clone();
            var config = new ComparisonConfig();
            config.MembersToIgnore.Add("Table"); //table should never be cloned
            var compareLogic = new CompareLogic(config);
            var result = compareLogic.Compare(test, clone);
            Assert.IsTrue(result.AreEqual, result.DifferencesString);
        }
    }
}
