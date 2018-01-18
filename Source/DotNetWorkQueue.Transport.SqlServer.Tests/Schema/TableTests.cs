#region Using

using DotNetWorkQueue.Transport.SqlServer.Schema;
using Xunit;

#endregion

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Schema
{
    public class TableTests
    {
        [Fact]
        public void Default()
        {
            var test = new Table("dbo", "test");
            Assert.Equal("dbo", test.Owner);
            Assert.Equal("test", test.Name);
        }
        [Fact]
        public void Set_PrimaryKey()
        {
            var c = new Constraint("ix_testing", ConstraintType.PrimaryKey, "testing");
            var test = new Table("dbo", "test");
            test.Constraints.Add(c);
            Assert.Equal(c, test.PrimaryKey);
        }
        [Fact]
        public void No_PrimaryKey()
        {
            var c = new Constraint("ix_testing", ConstraintType.Index, "testing");
            var test = new Table("dbo", "test");
            test.Constraints.Add(c);
            Assert.Null(test.PrimaryKey);
        }
        [Fact]
        public void No_PrimaryKey2()
        {
            var test = new Table("dbo", "test");
            Assert.Null(test.PrimaryKey);
        }
        [Fact]
        public void GetSet_Name()
        {
            var test = new Table("dbo", "test") {Name = "test1"};
            Assert.Equal("test1", test.Name);
        }
        [Fact]
        public void GetSet_Owner()
        {
            var test = new Table("dbo", "test") {Owner = "test1"};
            Assert.Equal("test1", test.Owner);
        }
        [Fact]
        public void Create_Column()
        {
            var c = new Column("testing", ColumnTypes.Bigint, true, null);
            var test = new Table("dbo", "test");
            test.Columns.Add(c);
            Assert.Contains(test.Columns.Items, item => item.Name == "testing");
        }
        [Fact]
        public void Create_Script()
        {
            var c = new Column("testing", ColumnTypes.Bigint, true, null);
            var cc = new Constraint("ix_testing", ConstraintType.Index, "testing");
            var test = new Table("dbo", "test");
            test.Constraints.Add(cc);
            test.Columns.Add(c);
            //set the table reference
            foreach (var ccc in test.Constraints)
            {
                ccc.Table = test.Info;
            }
            Assert.Contains("dbo", test.Script());
            Assert.Contains("test", test.Script());
        }
    }
}
