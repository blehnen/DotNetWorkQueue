#region Using

using DotNetWorkQueue.Transport.SQLite.Shared.Schema;
using KellermanSoftware.CompareNetObjects;
using Tynamix.ObjectFiller;
using Xunit;

#endregion

namespace DotNetWorkQueue.Transport.SQLite.Shared.Tests.Schema
{
    public class IdentityTests
    {
        [Fact]
        public void Script()
        {
            var test = new Identity();
            Assert.Contains("PRIMARY KEY AUTOINCREMENT", test.Script());
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
