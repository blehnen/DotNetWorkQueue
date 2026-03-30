using System;
using System.Linq;
using DotNetWorkQueue.Dashboard.Api;
using DotNetWorkQueue.Dashboard.Api.Configuration;
using DotNetWorkQueue.Dashboard.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Tests.Extensions
{
    [TestClass]
    public class DashboardExtensionsTests
    {
        [TestMethod]
        public void AddDotNetWorkQueueDashboard_Registers_DashboardOptions()
        {
            var services = new ServiceCollection();
            services.AddLogging();

            services.AddDotNetWorkQueueDashboard(options =>
            {
                options.EnableSwagger = false;
            });

            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<DashboardOptions>();
            Assert.IsNotNull(options);
            Assert.IsFalse(options.EnableSwagger);
        }

        [TestMethod]
        public void AddDotNetWorkQueueDashboard_Registers_IDashboardApi()
        {
            var services = new ServiceCollection();
            services.AddLogging();

            services.AddDotNetWorkQueueDashboard(options =>
            {
                options.EnableSwagger = false;
            });

            var provider = services.BuildServiceProvider();
            var dashboardApi = provider.GetRequiredService<IDashboardApi>();
            Assert.IsNotNull(dashboardApi);
        }

        [TestMethod]
        public void AddDotNetWorkQueueDashboard_Registers_IDashboardService()
        {
            var services = new ServiceCollection();
            services.AddLogging();

            services.AddDotNetWorkQueueDashboard(options =>
            {
                options.EnableSwagger = false;
            });

            var provider = services.BuildServiceProvider();
            var dashboardService = provider.GetRequiredService<IDashboardService>();
            Assert.IsNotNull(dashboardService);
        }

        [TestMethod]
        public void AddDotNetWorkQueueDashboard_Registers_IConsumerRegistry()
        {
            var services = new ServiceCollection();
            services.AddLogging();

            services.AddDotNetWorkQueueDashboard(options =>
            {
                options.EnableSwagger = false;
            });

            var provider = services.BuildServiceProvider();
            var consumerRegistry = provider.GetRequiredService<IConsumerRegistry>();
            Assert.IsNotNull(consumerRegistry);
        }

        [TestMethod]
        public void AddDotNetWorkQueueDashboard_Returns_ServiceCollection()
        {
            var services = new ServiceCollection();
            services.AddLogging();

            var result = services.AddDotNetWorkQueueDashboard(options =>
            {
                options.EnableSwagger = false;
            });

            Assert.AreSame(services, result);
        }

        [TestMethod]
        public void AddDotNetWorkQueueDashboard_Configures_Options_Via_Action()
        {
            var services = new ServiceCollection();
            services.AddLogging();

            services.AddDotNetWorkQueueDashboard(options =>
            {
                options.EnableSwagger = false;
                options.ReadOnly = true;
                options.ApiKey = "test-key";
                options.EnableConsumerTracking = false;
            });

            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<DashboardOptions>();

            Assert.IsFalse(options.EnableSwagger);
            Assert.IsTrue(options.ReadOnly);
            Assert.AreEqual("test-key", options.ApiKey);
            Assert.IsFalse(options.EnableConsumerTracking);
        }

        [TestMethod]
        public void AddDotNetWorkQueueDashboard_Registers_ConsumerPruningService_When_Tracking_Enabled()
        {
            var services = new ServiceCollection();
            services.AddLogging();

            services.AddDotNetWorkQueueDashboard(options =>
            {
                options.EnableSwagger = false;
                options.EnableConsumerTracking = true;
            });

            var hostedServiceRegistrations = services
                .Where(sd => sd.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService))
                .ToList();

            Assert.IsTrue(hostedServiceRegistrations.Count > 0,
                "ConsumerPruningService should be registered as IHostedService");
        }

        [TestMethod]
        public void AddDotNetWorkQueueDashboard_Does_Not_Register_ConsumerPruningService_When_Tracking_Disabled()
        {
            var services = new ServiceCollection();
            services.AddLogging();

            services.AddDotNetWorkQueueDashboard(options =>
            {
                options.EnableSwagger = false;
                options.EnableConsumerTracking = false;
            });

            var pruningServiceRegistrations = services
                .Where(sd => sd.ServiceType == typeof(Microsoft.Extensions.Hosting.IHostedService)
                    && sd.ImplementationType == typeof(DotNetWorkQueue.Dashboard.Api.Services.ConsumerPruningService))
                .ToList();

            Assert.AreEqual(0, pruningServiceRegistrations.Count,
                "ConsumerPruningService should not be registered when tracking is disabled");
        }
    }
}
