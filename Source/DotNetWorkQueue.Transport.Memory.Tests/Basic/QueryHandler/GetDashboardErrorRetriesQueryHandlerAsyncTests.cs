using System;
using DotNetWorkQueue.Transport.Memory;
using DotNetWorkQueue.Transport.Memory.Basic.QueryHandler;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.Memory.Tests.Basic.QueryHandler
{
    public class GetDashboardErrorRetriesQueryHandlerAsyncTests
    {
        [Fact]
        public void Create_Default()
        {
            Assert.NotNull(new GetDashboardErrorRetriesQueryHandlerAsync(Substitute.For<IDataStorage>()));
        }

        [Fact]
        public void Create_NullDataStorage_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new GetDashboardErrorRetriesQueryHandlerAsync(null));
        }
    }
}
