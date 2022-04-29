#region Using

using DotNetWorkQueue.Transport.SqlServer.Schema;
using KellermanSoftware.CompareNetObjects;
using Tynamix.ObjectFiller;
using Xunit;

#endregion

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Schema
{
    public class DefaultTests
    {
        [Fact]
        public void Default()
        {
            var test = new Default("test", "test1");
            Assert.Equal("test", test.Name);
            Assert.Equal("test1", test.Value);
        }
        [Fact]
        public void Script()
        {
            var test = new Default("test", "test1");
            Assert.Contains("test", test.Script());
            Assert.Contains("test1", test.Script());
        }

        [Fact]
        public void Clone()
        {
            var pFiller = new Filler<Default>();
            var test = pFiller.Create();
            var clone = test.Clone();
            var compareLogic = new CompareLogic();
            var result = compareLogic.Compare(test, clone);
            Assert.True(result.AreEqual, result.DifferencesString);
        }

        [Fact]
        public void CloneNewName()
        {
            var pFiller = new Filler<Default>();
            var test = pFiller.Create();
            var clone = test.Clone("ANewName");
            var compareLogic = new CompareLogic();
            compareLogic.Config.MembersToIgnore.Add("Name");
            var result = compareLogic.Compare(test, clone);
            Assert.True(result.AreEqual, result.DifferencesString);
            Assert.NotEqual(test.Name, clone.Name);
        }
    }
}
