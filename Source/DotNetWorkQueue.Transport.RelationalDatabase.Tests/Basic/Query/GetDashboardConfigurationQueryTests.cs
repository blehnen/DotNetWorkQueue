using DotNetWorkQueue.Transport.Shared.Basic.Query;
using Xunit;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Tests.Basic.Query
{
    public class GetDashboardConfigurationQueryTests
    {
        [Fact]
        public void Create_Default()
        {
            var test = new GetDashboardConfigurationQuery();
            Assert.NotNull(test);
        }
    }
}
