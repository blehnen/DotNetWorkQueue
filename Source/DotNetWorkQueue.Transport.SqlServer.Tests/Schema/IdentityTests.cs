#region Using

using DotNetWorkQueue.Transport.SqlServer.Schema;
using KellermanSoftware.CompareNetObjects;
using Tynamix.ObjectFiller;
using Xunit;

#endregion

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Schema
{
    public class IdentityTests
    {
        [Fact]
        public void Default()
        {
            var test = new Identity(1, 2);
            Assert.Equal(1, test.Seed);
            Assert.Equal(2, test.Increment);
        }
        [Fact]
        public void Script()
        {
            var test = new Identity(1, 2);
            Assert.Contains("1", test.Script());
            Assert.Contains("2", test.Script());
        }
        [Fact]
        public void Clone()
        {
            var pFiller = new Filler<Identity>();
            var test = pFiller.Create();
            var clone = test.Clone();
            var compareLogic = new CompareLogic();
            var result = compareLogic.Compare(test, clone);
            Assert.True(result.AreEqual, result.DifferencesString);
        }
    }
}
