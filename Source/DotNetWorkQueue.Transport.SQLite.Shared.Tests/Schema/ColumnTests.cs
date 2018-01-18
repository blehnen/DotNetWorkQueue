#region Using

using System;
using DotNetWorkQueue.Transport.SQLite.Shared.Schema;
using KellermanSoftware.CompareNetObjects;
using Tynamix.ObjectFiller;
using Xunit;

#endregion

namespace DotNetWorkQueue.Transport.SQLite.Shared.Tests.Schema
{
    public class ColumnTests
    {
        [Fact]
        public void GetSet_Nullable()
        {
            var test = new Column();
            var c = test.Nullable;
            test.Nullable = !c;
            Assert.Equal(!c, test.Nullable);
        }
        [Fact]
        public void GetSet_Default()
        {
            var test = new Column();
            var d = new Default("", "");
            test.Default = d;
            Assert.Equal(d, test.Default);
        }
        [Fact]
        public void GetSet_Identity()
        {
            var test = new Column();
            var d = new Identity();
            test.Identity = d;
            Assert.Equal(d, test.Identity);
        }
        [Fact]
        public void GetSet_Length()
        {
            var test = new Column();
            var c = test.Length;
            test.Length = c + 1;
            Assert.Equal(c + 1, test.Length);
        }
        [Fact]
        public void GetSet_Position()
        {
            var test = new Column();
            var c = test.Position;
            test.Position = c + 1;
            Assert.Equal(c + 1, test.Position);
        }
        [Fact]
        public void GetSet_Precision()
        {
            var b = new byte();
            var test = new Column {Precision = b};
            Assert.Equal(b, test.Precision);
        }
        [Fact]
        public void GetSet_Scale()
        {
            var test = new Column();
            var c = test.Scale;
            test.Scale = c + 1;
            Assert.Equal(c + 1, test.Scale);
        }
        [Fact]
        public void GetSet_Name()
        {
            var test = new Column {Name = "test"};
            Assert.Equal("test", test.Name);
        }
        [Fact]
        public void GetSet_Types()
        {
            var test = new Column();
            foreach (ColumnTypes type in Enum.GetValues(typeof(ColumnTypes)))
            {
                test.Type = type;
                Assert.Equal(type, test.Type);
            }
        }

        [Fact]
        public void Clone()
        {
            var pFiller = new Filler<Column>();
            pFiller.Setup()
               .OnType<Identity>().Use(() => new Identity());
            var test = pFiller.Create();
            var clone = test.Clone();
            var compareLogic = new CompareLogic();
            var result = compareLogic.Compare(test, clone);
            Assert.True(result.AreEqual, result.DifferencesString);
        }
    }
}
