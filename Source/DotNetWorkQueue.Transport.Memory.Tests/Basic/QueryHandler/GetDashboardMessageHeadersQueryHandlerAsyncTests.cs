using System;
using DotNetWorkQueue.Transport.Memory;
using DotNetWorkQueue.Transport.Memory.Basic.QueryHandler;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Memory.Tests.Basic.QueryHandler
{
    [TestClass]
    public class GetDashboardMessageHeadersQueryHandlerAsyncTests
    {
        [TestMethod]
        public void Create_Default()
        {
            Assert.IsNotNull(new GetDashboardMessageHeadersQueryHandlerAsync(
                Substitute.For<IDataStorage>(),
                Substitute.For<IInternalSerializer>()));
        }

        [TestMethod]
        public void Create_NullDataStorage_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new GetDashboardMessageHeadersQueryHandlerAsync(
                null,
                Substitute.For<IInternalSerializer>()));
        }

        [TestMethod]
        public void Create_NullInternalSerializer_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new GetDashboardMessageHeadersQueryHandlerAsync(
                Substitute.For<IDataStorage>(),
                null));
        }
    }
}
