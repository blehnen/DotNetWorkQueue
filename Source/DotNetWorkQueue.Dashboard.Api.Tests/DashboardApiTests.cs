using System;
using System.Linq;
using DotNetWorkQueue.Dashboard.Api.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Tests
{
    [TestClass]
    public class DashboardApiTests
    {
        [TestMethod]
        public void Connections_Empty_When_No_Registrations()
        {
            var options = new DashboardOptions();
            using var api = new DashboardApi(options, NullLogger<DashboardApi>.Instance);

            Assert.IsFalse((api.Connections).Any());
        }

        [TestMethod]
        public void FindQueue_Returns_Null_For_Unknown_Id()
        {
            var options = new DashboardOptions();
            using var api = new DashboardApi(options, NullLogger<DashboardApi>.Instance);

            var result = api.FindQueue(Guid.NewGuid());

            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetQueueContainer_Throws_For_Unknown_Id()
        {
            var options = new DashboardOptions();
            using var api = new DashboardApi(options, NullLogger<DashboardApi>.Instance);

            var act = () => api.GetQueueContainer(Guid.NewGuid());

            Assert.Throws<InvalidOperationException>(act);
        }

        [TestMethod]
        public void Dispose_Can_Be_Called_Multiple_Times()
        {
            var options = new DashboardOptions();
            var api = new DashboardApi(options, NullLogger<DashboardApi>.Instance);

            api.Dispose();
            api.Dispose(); // should not throw
        }

        [TestMethod]
        public void FindQueue_Throws_After_Dispose()
        {
            var options = new DashboardOptions();
            var api = new DashboardApi(options, NullLogger<DashboardApi>.Instance);
            api.Dispose();

            var act = () => api.FindQueue(Guid.NewGuid());

            Assert.Throws<ObjectDisposedException>(act);
        }

        [TestMethod]
        public void GetQueueContainer_Throws_After_Dispose()
        {
            var options = new DashboardOptions();
            var api = new DashboardApi(options, NullLogger<DashboardApi>.Instance);
            api.Dispose();

            var act = () => api.GetQueueContainer(Guid.NewGuid());

            Assert.Throws<ObjectDisposedException>(act);
        }
    }
}
