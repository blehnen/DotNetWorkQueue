using System;
using DotNetWorkQueue.Transport.Memory;
using DotNetWorkQueue.Transport.Memory.Basic.QueryHandler;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.Memory.Tests.Basic.QueryHandler
{
    public class GetDashboardJobsQueryHandlerAsyncTests
    {
        [Fact]
        public void Create_Default()
        {
            Assert.NotNull(new GetDashboardJobsQueryHandlerAsync(Substitute.For<IDataStorage>()));
        }

        [Fact]
        public void Create_NullDataStorage_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new GetDashboardJobsQueryHandlerAsync(null));
        }
    }
}
