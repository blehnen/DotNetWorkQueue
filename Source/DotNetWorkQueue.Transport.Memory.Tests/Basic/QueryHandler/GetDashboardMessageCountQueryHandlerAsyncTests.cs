using System;
using DotNetWorkQueue.Transport.Memory;
using DotNetWorkQueue.Transport.Memory.Basic.QueryHandler;
using NSubstitute;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Memory.Tests.Basic.QueryHandler
{
    [TestClass]
    public class GetDashboardMessageCountQueryHandlerAsyncTests
    {
        [TestMethod]
        public void Create_Default()
        {
            Assert.IsNotNull(new GetDashboardMessageCountQueryHandlerAsync(Substitute.For<IDataStorage>()));
        }

        [TestMethod]
        public void Create_NullDataStorage_Throws()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new GetDashboardMessageCountQueryHandlerAsync(null));
        }
    }
}
