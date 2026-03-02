using System;
using DotNetWorkQueue.Transport.Memory;
using DotNetWorkQueue.Transport.Memory.Basic.QueryHandler;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.Memory.Tests.Basic.QueryHandler
{
    public class GetDashboardMessageHeadersQueryHandlerAsyncTests
    {
        [Fact]
        public void Create_Default()
        {
            Assert.NotNull(new GetDashboardMessageHeadersQueryHandlerAsync(
                Substitute.For<IDataStorage>(),
                Substitute.For<IInternalSerializer>()));
        }

        [Fact]
        public void Create_NullDataStorage_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new GetDashboardMessageHeadersQueryHandlerAsync(
                null,
                Substitute.For<IInternalSerializer>()));
        }

        [Fact]
        public void Create_NullInternalSerializer_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new GetDashboardMessageHeadersQueryHandlerAsync(
                Substitute.For<IDataStorage>(),
                null));
        }
    }
}
