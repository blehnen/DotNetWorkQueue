#region Using

using System.Collections.Generic;
using DotNetWorkQueue.Transport.SqlServer.Schema;
using KellermanSoftware.CompareNetObjects;
using Tynamix.ObjectFiller;
using Xunit;

#endregion

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Schema
{
    public class ConstraintTests
    {
        [Fact]
        public void GetSet_Clustered()
        {
            var test = new Constraint("test", ConstraintType.Constraint, new List<string>());
            var c = test.Clustered;
            test.Clustered = !c;
            Assert.Equal(!c, test.Clustered);
        }
        [Fact]
        public void GetSet_Unique()
        {
            var test = new Constraint("test", ConstraintType.Constraint, new List<string>());
            var c = test.Unique;
            test.Unique = !c;
            Assert.Equal(!c, test.Unique);
        }
        [Fact]
        public void GetSet_Columns()
        {
            var columns = new List<string> { "test" };
            var test = new Constraint("test", ConstraintType.Constraint, columns);
            Assert.Equal(columns, test.Columns);
        }
        [Fact]
        public void GetSet_Name()
        {
            var test = new Constraint("test", ConstraintType.Constraint, new List<string>()) { Name = "test1" };
            Assert.Equal("test1", test.Name);
        }
        [Fact]
        public void GetSet_ConstraintType()
        {
            var test = new Constraint("test", ConstraintType.Constraint, new List<string>());
            Assert.Equal(ConstraintType.Constraint, test.Type);
        }

        [Fact]
        public void Script()
        {
            var test = new Constraint("test", ConstraintType.PrimaryKey, new List<string>());
            Assert.Contains("test", test.Script());
        }

        [Fact]
        public void Clone()
        {
            var pFiller = new Filler<Constraint>();
            var test = pFiller.Create();
            var clone = test.Clone();
            var config = new ComparisonConfig();
            config.MembersToIgnore.Add("Table"); //table should never be cloned
            var compareLogic = new CompareLogic(config);
            var result = compareLogic.Compare(test, clone);
            Assert.True(result.AreEqual, result.DifferencesString);
        }
    }
}
