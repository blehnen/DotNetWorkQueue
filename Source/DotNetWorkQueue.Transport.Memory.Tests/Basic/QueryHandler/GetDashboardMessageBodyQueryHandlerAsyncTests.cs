using System;
using DotNetWorkQueue.Transport.Memory;
using DotNetWorkQueue.Transport.Memory.Basic.QueryHandler;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Memory.Tests.Basic.QueryHandler
{
    [TestClass]
    public class GetDashboardMessageBodyQueryHandlerAsyncTests
    {
        [TestMethod]
        public void Create_Default()
        {
            Assert.IsNotNull(new GetDashboardMessageBodyQueryHandlerAsync(
                Substitute.For<IDataStorage>(),
                Substitute.For<ICompositeSerialization>(),
                Substitute.For<IHeaders>()));
        }

        [TestMethod]
        public void Create_NullDataStorage_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new GetDashboardMessageBodyQueryHandlerAsync(
                null,
                Substitute.For<ICompositeSerialization>(),
                Substitute.For<IHeaders>()));
        }

        [TestMethod]
        public void Create_NullSerialization_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new GetDashboardMessageBodyQueryHandlerAsync(
                Substitute.For<IDataStorage>(),
                null,
                Substitute.For<IHeaders>()));
        }

        [TestMethod]
        public void Create_NullHeaders_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new GetDashboardMessageBodyQueryHandlerAsync(
                Substitute.For<IDataStorage>(),
                Substitute.For<ICompositeSerialization>(),
                null));
        }
    }
}
