#region Using
using System.Linq;
using DotNetWorkQueue.Transport.SQLite.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace DotNetWorkQueue.Transport.SQLite.Tests.Schema
{
    [TestClass]
    public class ColumnsTests
    {
        [TestMethod]
        public void Add_Column()
        {
            var test = new Columns();
            test.Add(new Column("testing", ColumnTypes.Integer, true, null));
            Assert.Contains(item => item.Name == "testing", test.Items);
        }
        [TestMethod]
        public void Remove_Column()
        {
            var test = new Columns();
            var column = new Column("testing", ColumnTypes.Integer, true, null);
            test.Add(column);
            test.Remove(column);
            Assert.DoesNotContain(item => item.Name == "testing", test.Items);
        }
        [TestMethod]
        public void Script()
        {
            var test = new Columns();
            test.Add(new Column("testing", ColumnTypes.Integer, true, null));
            StringAssert.Contains(test.Script(), "testing");
        }
    }
}
