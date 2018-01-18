#region Using

using DotNetWorkQueue.Transport.SQLite.Shared.Schema;
using Xunit;

#endregion

namespace DotNetWorkQueue.Transport.SQLite.Shared.Tests.Schema
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
    }
}
