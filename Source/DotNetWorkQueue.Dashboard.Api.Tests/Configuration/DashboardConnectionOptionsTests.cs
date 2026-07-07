using System;
using System.Linq;
using DotNetWorkQueue.Dashboard.Api.Configuration;
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
            Assert.HasCount(1, opts.Queues);
            Assert.AreEqual("TestQueue", opts.Queues[0].QueueName);
        }

        [TestMethod]
        public void AddQueue_Without_Interceptors_Has_Null_Config()
        {
            var opts = new DashboardConnectionOptions();
            opts.AddQueue("TestQueue");
            Assert.IsNull(opts.Queues[0].InterceptorConfiguration);
        }

        [TestMethod]
        public void AddQueue_With_Interceptors_Stores_Config()
        {
            var opts = new DashboardConnectionOptions();
            Action<IContainer> config = c => { };
            opts.AddQueue("TestQueue", config);
            Assert.IsNotNull(opts.Queues[0].InterceptorConfiguration);
            Assert.AreSame(config, opts.Queues[0].InterceptorConfiguration);
        }

        [TestMethod]
        public void AddQueue_Multiple_Queues()
        {
            var opts = new DashboardConnectionOptions();
            opts.AddQueue("Queue1");
            opts.AddQueue("Queue2");
            opts.AddQueue("Queue3");
            Assert.HasCount(3, opts.Queues);
        }

        [TestMethod]
        public void AddQueueWithProfile_Stores_Profile_Name()
        {
            var opts = new DashboardConnectionOptions();
            opts.AddQueueWithProfile("TestQueue", "encrypted");
            Assert.HasCount(1, opts.Queues);
            Assert.AreEqual("TestQueue", opts.Queues[0].QueueName);
            Assert.AreEqual("encrypted", opts.Queues[0].InterceptorProfile);
            Assert.IsNull(opts.Queues[0].InterceptorConfiguration);
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
            Assert.HasCount(1, opts.Queues);
            Assert.AreEqual("TestQueue", opts.Queues[0].QueueName);
            Assert.AreSame(interceptors, opts.Queues[0].Interceptors);
            Assert.IsNull(opts.Queues[0].InterceptorConfiguration);
        }
    }
}
