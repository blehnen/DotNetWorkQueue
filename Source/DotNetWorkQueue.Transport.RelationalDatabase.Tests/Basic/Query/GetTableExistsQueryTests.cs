using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Query
{
    public class GetTableExistsQueryTests
    {
        [Fact]
        public void Create_Default()
        {
            var test = new GetTableExistsQuery("test1", "test2");
            Assert.Equal("test1", test.ConnectionString);
            Assert.Equal("test2", test.TableName);
        }
    }
}
