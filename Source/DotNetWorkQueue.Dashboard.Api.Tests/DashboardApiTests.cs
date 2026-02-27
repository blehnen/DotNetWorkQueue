using System;
using DotNetWorkQueue.Dashboard.Api.Configuration;
using FluentAssertions;
using Xunit;

namespace DotNetWorkQueue.Dashboard.Api.Tests
{
    public class DashboardApiTests
    {
        [Fact]
        public void Connections_Empty_When_No_Registrations()
        {
            var options = new DashboardOptions();
            using var api = new DashboardApi(options);

            api.Connections.Should().BeEmpty();
        }

        [Fact]
        public void FindQueue_Returns_Null_For_Unknown_Id()
        {
            var options = new DashboardOptions();
            using var api = new DashboardApi(options);

            var result = api.FindQueue(Guid.NewGuid());

            result.Should().BeNull();
        }

        [Fact]
        public void GetQueueContainer_Throws_For_Unknown_Id()
        {
            var options = new DashboardOptions();
            using var api = new DashboardApi(options);

            var act = () => api.GetQueueContainer(Guid.NewGuid());

            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Dispose_Can_Be_Called_Multiple_Times()
        {
            var options = new DashboardOptions();
            var api = new DashboardApi(options);

            api.Dispose();
            api.Dispose(); // should not throw
        }

        [Fact]
        public void FindQueue_Throws_After_Dispose()
        {
            var options = new DashboardOptions();
            var api = new DashboardApi(options);
            api.Dispose();

            var act = () => api.FindQueue(Guid.NewGuid());

            act.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public void GetQueueContainer_Throws_After_Dispose()
        {
            var options = new DashboardOptions();
            var api = new DashboardApi(options);
            api.Dispose();

            var act = () => api.GetQueueContainer(Guid.NewGuid());

            act.Should().Throw<ObjectDisposedException>();
        }
    }
}
