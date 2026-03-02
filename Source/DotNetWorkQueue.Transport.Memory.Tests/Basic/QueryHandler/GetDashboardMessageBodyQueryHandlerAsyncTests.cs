using System;
using DotNetWorkQueue.Transport.Memory;
using DotNetWorkQueue.Transport.Memory.Basic.QueryHandler;
using NSubstitute;
using Xunit;

namespace DotNetWorkQueue.Transport.Memory.Tests.Basic.QueryHandler
{
    public class GetDashboardMessageBodyQueryHandlerAsyncTests
    {
        [Fact]
        public void Create_Default()
        {
            Assert.NotNull(new GetDashboardMessageBodyQueryHandlerAsync(
                Substitute.For<IDataStorage>(),
                Substitute.For<ICompositeSerialization>(),
                Substitute.For<IHeaders>()));
        }

        [Fact]
        public void Create_NullDataStorage_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new GetDashboardMessageBodyQueryHandlerAsync(
                null,
                Substitute.For<ICompositeSerialization>(),
                Substitute.For<IHeaders>()));
        }

        [Fact]
        public void Create_NullSerialization_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new GetDashboardMessageBodyQueryHandlerAsync(
                Substitute.For<IDataStorage>(),
                null,
                Substitute.For<IHeaders>()));
        }

        [Fact]
        public void Create_NullHeaders_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new GetDashboardMessageBodyQueryHandlerAsync(
                Substitute.For<IDataStorage>(),
                Substitute.For<ICompositeSerialization>(),
                null));
        }
    }
}
