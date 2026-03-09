#region Using
using System.Linq;
using DotNetWorkQueue.Transport.SQLite.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace DotNetWorkQueue.Transport.SQLite.Tests.Schema
{
    [TestClass]
    public class TableTests
    {
        [TestMethod]
        public void Default()
        {
            var test = new Table("test");
            Assert.AreEqual("test", test.Name);
        }
        [TestMethod]
        public void Set_PrimaryKey()
        {
            var c = new Constraint("ix_testing", ConstraintType.PrimaryKey, "testing");
            var test = new Table("test");
            test.Constraints.Add(c);
            Assert.AreEqual(c, test.PrimaryKey);
        }
        [TestMethod]
        public void No_PrimaryKey()
        {
            var c = new Constraint("ix_testing", ConstraintType.Index, "testing");
            var test = new Table("test");
            test.Constraints.Add(c);
            Assert.IsNull(test.PrimaryKey);
        }
        [TestMethod]
        public void No_PrimaryKey2()
        {
            var test = new Table("test");
            Assert.IsNull(test.PrimaryKey);
        }
        [TestMethod]
        public void GetSet_Name()
        {
            var test = new Table("test") { Name = "test1" };
            Assert.AreEqual("test1", test.Name);
        }
        [TestMethod]
        public void Create_Column()
        {
            var c = new Column("testing", ColumnTypes.Integer, true, null);
            var test = new Table("test");
            test.Columns.Add(c);
            Assert.IsTrue(test.Columns.Items.Any(item => item.Name == "testing"));
        }
        [TestMethod]
        public void Create_Script()
        {
            var c = new Column("testing", ColumnTypes.Integer, true, null);
            var cc = new Constraint("ix_testing", ConstraintType.Index, "testing");
            var test = new Table("test");
            test.Constraints.Add(cc);
            test.Columns.Add(c);
            //set the table reference
            foreach (var ccc in test.Constraints)
            {
                ccc.Table = test.Info;
            }
            StringAssert.Contains(test.Script(), "test");
        }
    }
}
