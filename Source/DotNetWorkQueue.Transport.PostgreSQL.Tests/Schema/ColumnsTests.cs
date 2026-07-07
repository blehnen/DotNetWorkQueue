#region Using

using System.Linq;
using DotNetWorkQueue.Transport.PostgreSQL.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;

#endregion

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Schema
{
    [TestClass]
    public class ColumnsTests
    {
        [TestMethod]
        public void Add_Column()
        {
            var test = new Columns();
            test.Add(new Column("testing", ColumnTypes.Bigint, true));
            Assert.Contains(item => item.Name == "testing", test.Items);
        }
        [TestMethod]
        public void Remove_Column()
        {
            var test = new Columns();
            var column = new Column("testing", ColumnTypes.Bigint, true);
            test.Add(column);
            test.Remove(column);
            Assert.DoesNotContain(item => item.Name == "testing", test.Items);
        }
        [TestMethod]
        public void Script()
        {
            var test = new Columns();
            test.Add(new Column("testing", ColumnTypes.Bigint, true));
            StringAssert.Contains(test.Script(), "testing");
        }
    }
}
