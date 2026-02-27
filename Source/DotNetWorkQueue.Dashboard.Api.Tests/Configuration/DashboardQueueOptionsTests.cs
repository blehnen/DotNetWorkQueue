using DotNetWorkQueue.Dashboard.Api.Configuration;
using FluentAssertions;
using Xunit;

namespace DotNetWorkQueue.Dashboard.Api.Tests.Configuration
{
    public class DashboardQueueOptionsTests
    {
        [Fact]
        public void QueueName_Can_Be_Set()
        {
            var opts = new DashboardQueueOptions { QueueName = "MyQueue" };
            opts.QueueName.Should().Be("MyQueue");
        }

        [Fact]
        public void InterceptorConfiguration_Defaults_To_Null()
        {
            var opts = new DashboardQueueOptions();
            opts.InterceptorConfiguration.Should().BeNull();
        }
    }
}
