#region Using

using DotNetWorkQueue.Transport.PostgreSQL.Schema;
using Xunit;

#endregion

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Schema
{
    public class TableTests
    {
        [Fact]
        public void Default()
        {
            var test = new Table("test");
            Assert.Equal("test", test.Name);
        }
        [Fact]
        public void Set_PrimaryKey()
        {
            var c = new Constraint("ix_testing", ConstraintType.PrimaryKey, "testing");
            var test = new Table("test");
            test.Constraints.Add(c);
            Assert.Equal(c, test.PrimaryKey);
        }
        [Fact]
        public void No_PrimaryKey()
        {
            var c = new Constraint("ix_testing", ConstraintType.Index, "testing");
            var test = new Table("test");
            test.Constraints.Add(c);
            Assert.Null(test.PrimaryKey);
        }
        [Fact]
        public void No_PrimaryKey2()
        {
            var test = new Table("test");
            Assert.Null(test.PrimaryKey);
        }
        [Fact]
        public void GetSet_Name()
        {
            var test = new Table("test") {Name = "test1"};
            Assert.Equal("test1", test.Name);
        }
        [Fact]
        public void Create_Column()
        {
            var c = new Column("testing", ColumnTypes.Bigint, true);
            var test = new Table("test");
            test.Columns.Add(c);
            Assert.Contains(test.Columns.Items, item => item.Name == "testing");
        }
        [Fact]
        public void Create_Script()
        {
            var c = new Column("testing", ColumnTypes.Bigint, true);
            var cc = new Constraint("ix_testing", ConstraintType.Index, "testing");
            var test = new Table("test");
            test.Constraints.Add(cc);
            test.Columns.Add(c);
            //set the table reference
            foreach (var ccc in test.Constraints)
            {
                ccc.Table = test.Info;
            }
            Assert.Contains("test", test.Script());
        }
    }
}
