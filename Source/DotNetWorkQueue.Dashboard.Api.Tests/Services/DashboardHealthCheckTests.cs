using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetWorkQueue.Dashboard.Api.Services;
using FluentAssertions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace DotNetWorkQueue.Dashboard.Api.Tests.Services
{
    [TestClass]
    public class DashboardHealthCheckTests
    {
        [TestMethod]
        public async Task CheckHealthAsync_When_Service_Healthy_Returns_Healthy()
        {
            var api = Substitute.For<IDashboardApi>();
            api.Connections.Returns(new Dictionary<Guid, DashboardConnectionInfo>());
            var check = new DashboardHealthCheck(api);

            var result = await check.CheckHealthAsync(new HealthCheckContext());

            result.Status.Should().Be(HealthStatus.Healthy);
            result.Data.Should().ContainKey("uptime");
            result.Data.Should().ContainKey("connections");
        }

        [TestMethod]
        public async Task CheckHealthAsync_When_Service_Disposed_Returns_Unhealthy()
        {
            var api = Substitute.For<IDashboardApi>();
            api.Connections.Throws(new ObjectDisposedException("test"));
            var check = new DashboardHealthCheck(api);

            var result = await check.CheckHealthAsync(new HealthCheckContext());

            result.Status.Should().Be(HealthStatus.Unhealthy);
            result.Description.Should().Contain("disposed");
        }

        [TestMethod]
        public async Task CheckHealthAsync_When_Exception_Returns_Unhealthy()
        {
            var api = Substitute.For<IDashboardApi>();
            api.Connections.Throws(new InvalidOperationException("broken"));
            var check = new DashboardHealthCheck(api);

            var result = await check.CheckHealthAsync(new HealthCheckContext());

            result.Status.Should().Be(HealthStatus.Unhealthy);
            result.Description.Should().Contain("failed");
        }
    }
}
