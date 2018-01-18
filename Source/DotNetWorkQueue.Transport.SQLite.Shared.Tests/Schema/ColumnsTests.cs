#region Using

using DotNetWorkQueue.Transport.SQLite.Shared.Schema;
using Xunit;

#endregion

namespace DotNetWorkQueue.Transport.SQLite.Shared.Tests.Schema
{
    public class ColumnsTests
    {
        [Fact]
        public void Add_Column()
        {
            var test = new Columns();
            test.Add(new Column("testing", ColumnTypes.Integer, true, null));
            Assert.Contains(test.Items, item => item.Name == "testing");
        }
        [Fact]
        public void Remove_Column()
        {
            var test = new Columns();
            var column = new Column("testing", ColumnTypes.Integer, true, null);
            test.Add(column);
            test.Remove(column);
            Assert.DoesNotContain(test.Items, item => item.Name == "testing");
        }
        [Fact]
        public void Script()
        {
            var test = new Columns();
            test.Add(new Column("testing", ColumnTypes.Integer, true, null));
            Assert.Contains("testing", test.Script());
        }
    }
}
