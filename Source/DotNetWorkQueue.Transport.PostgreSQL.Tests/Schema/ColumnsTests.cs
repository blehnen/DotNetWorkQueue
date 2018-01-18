#region Using

using DotNetWorkQueue.Transport.PostgreSQL.Schema;
using Xunit;

#endregion

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Schema
{
    public class ColumnsTests
    {
        [Fact]
        public void Add_Column()
        {
            var test = new Columns();
            test.Add(new Column("testing", ColumnTypes.Bigint, true));
            Assert.Contains(test.Items, item => item.Name == "testing");
        }
        [Fact]
        public void Remove_Column()
        {
            var test = new Columns();
            var column = new Column("testing", ColumnTypes.Bigint, true);
            test.Add(column);
            test.Remove(column);
            Assert.DoesNotContain(test.Items, item => item.Name == "testing");
        }
        [Fact]
        public void Script()
        {
            var test = new Columns();
            test.Add(new Column("testing", ColumnTypes.Bigint, true));
            Assert.Contains("testing", test.Script());
        }
    }
}
