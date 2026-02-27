using System;
using DotNetWorkQueue.Dashboard.Api.Configuration;
using FluentAssertions;
using Xunit;

namespace DotNetWorkQueue.Dashboard.Api.Tests.Configuration
{
    public class DashboardConnectionOptionsTests
    {
        [Fact]
        public void AddQueue_Adds_To_List()
        {
            var opts = new DashboardConnectionOptions();
            opts.AddQueue("TestQueue");
            opts.Queues.Should().HaveCount(1);
            opts.Queues[0].QueueName.Should().Be("TestQueue");
        }

        [Fact]
        public void AddQueue_Without_Interceptors_Has_Null_Config()
        {
            var opts = new DashboardConnectionOptions();
            opts.AddQueue("TestQueue");
            opts.Queues[0].InterceptorConfiguration.Should().BeNull();
        }

        [Fact]
        public void AddQueue_With_Interceptors_Stores_Config()
        {
            var opts = new DashboardConnectionOptions();
            Action<IContainer> config = c => { };
            opts.AddQueue("TestQueue", config);
            opts.Queues[0].InterceptorConfiguration.Should().NotBeNull();
            opts.Queues[0].InterceptorConfiguration.Should().BeSameAs(config);
        }

        [Fact]
        public void AddQueue_Multiple_Queues()
        {
            var opts = new DashboardConnectionOptions();
            opts.AddQueue("Queue1");
            opts.AddQueue("Queue2");
            opts.AddQueue("Queue3");
            opts.Queues.Should().HaveCount(3);
        }
    }
}
