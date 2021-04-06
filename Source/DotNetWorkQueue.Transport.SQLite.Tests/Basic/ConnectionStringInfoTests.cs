
using DotNetWorkQueue.Transport.SQLite.Basic;
using Xunit;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic
{
    public class ConnectionStringInfoTests
    {
        [Fact]
        public void Create_ConnectionStringInfo()
        {
            var test = new ConnectionStringInfo(false, @"c:\test\temp.db3");
            Assert.False(test.IsInMemory);
            Assert.Equal(@"c:\test\temp.db3", test.FileName);
            Assert.True(test.IsValid);
        }
        [Fact]  
        public void Create_InMemoryIsValid()
        {
            var test = new ConnectionStringInfo(true, string.Empty);
            Assert.True(test.IsValid);
        }
    }
}
