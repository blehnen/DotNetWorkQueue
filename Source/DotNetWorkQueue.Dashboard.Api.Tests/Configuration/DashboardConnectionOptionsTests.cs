using System;
using DotNetWorkQueue.Dashboard.Api.Configuration;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Tests.Configuration
{
    [TestClass]
    public class DashboardConnectionOptionsTests
    {
        [TestMethod]
        public void AddQueue_Adds_To_List()
        {
            var opts = new DashboardConnectionOptions();
            opts.AddQueue("TestQueue");
            opts.Queues.Should().HaveCount(1);
            opts.Queues[0].QueueName.Should().Be("TestQueue");
        }

        [TestMethod]
        public void AddQueue_Without_Interceptors_Has_Null_Config()
        {
            var opts = new DashboardConnectionOptions();
            opts.AddQueue("TestQueue");
            opts.Queues[0].InterceptorConfiguration.Should().BeNull();
        }

        [TestMethod]
        public void AddQueue_With_Interceptors_Stores_Config()
        {
            var opts = new DashboardConnectionOptions();
            Action<IContainer> config = c => { };
            opts.AddQueue("TestQueue", config);
            opts.Queues[0].InterceptorConfiguration.Should().NotBeNull();
            opts.Queues[0].InterceptorConfiguration.Should().BeSameAs(config);
        }

        [TestMethod]
        public void AddQueue_Multiple_Queues()
        {
            var opts = new DashboardConnectionOptions();
            opts.AddQueue("Queue1");
            opts.AddQueue("Queue2");
            opts.AddQueue("Queue3");
            opts.Queues.Should().HaveCount(3);
        }

        [TestMethod]
        public void AddQueueWithProfile_Stores_Profile_Name()
        {
            var opts = new DashboardConnectionOptions();
            opts.AddQueueWithProfile("TestQueue", "encrypted");
            opts.Queues.Should().HaveCount(1);
            opts.Queues[0].QueueName.Should().Be("TestQueue");
            opts.Queues[0].InterceptorProfile.Should().Be("encrypted");
            opts.Queues[0].InterceptorConfiguration.Should().BeNull();
        }

        [TestMethod]
        public void AddQueue_With_InterceptorOptions_Stores_Options()
        {
            var opts = new DashboardConnectionOptions();
            var interceptors = new DashboardInterceptorOptions
            {
                GZip = new GZipInterceptorOptions { MinimumSize = 200 }
            };
            opts.AddQueue("TestQueue", interceptors);
            opts.Queues.Should().HaveCount(1);
            opts.Queues[0].QueueName.Should().Be("TestQueue");
            opts.Queues[0].Interceptors.Should().BeSameAs(interceptors);
            opts.Queues[0].InterceptorConfiguration.Should().BeNull();
        }
    }
}
