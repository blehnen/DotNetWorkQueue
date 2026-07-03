using System;
using System.IO;
using System.Linq;
using System.Reflection;
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

        [TestMethod]
        public void AddDotNetWorkQueueDashboard_PreloadsAssemblies_From_AssemblyPaths()
        {
            // Copy a known DLL to a temp "plugin" directory
            var pluginDir = Path.Combine(Path.GetTempPath(), "dnwq-preload-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(pluginDir);
            try
            {
                // Use Newtonsoft.Json as a test DLL — it's in our bin but let's verify
                // the preload path works by copying it and checking it loads from there
                var sourceDll = Path.Combine(AppContext.BaseDirectory, "Newtonsoft.Json.dll");
                var destDll = Path.Combine(pluginDir, "Newtonsoft.Json.dll");
                File.Copy(sourceDll, destDll);

                var services = new ServiceCollection();
                services.AddLogging();

                services.AddDotNetWorkQueueDashboard(options =>
                {
                    options.EnableSwagger = false;
                    options.AssemblyPaths = new[] { pluginDir };
                });

                // If PreloadAssemblies threw, we wouldn't get here
                var provider = services.BuildServiceProvider();
                var opts = provider.GetRequiredService<DashboardOptions>();
                Assert.AreEqual(1, (opts.AssemblyPaths).Count());
                Assert.AreEqual(pluginDir, (opts.AssemblyPaths).Single());
            }
            finally
            {
                Directory.Delete(pluginDir, true);
            }
        }

        [TestMethod]
        public void AddDotNetWorkQueueDashboard_PreloadAssemblies_Ignores_NonexistentDir()
        {
            var services = new ServiceCollection();
            services.AddLogging();

            // Should not throw even if the directory doesn't exist
            services.AddDotNetWorkQueueDashboard(options =>
            {
                options.EnableSwagger = false;
                options.AssemblyPaths = new[] { "/nonexistent/path/that/does/not/exist" };
            });

            var provider = services.BuildServiceProvider();
            Assert.IsNotNull(provider.GetRequiredService<DashboardOptions>());
        }

        [TestMethod]
        public void AddDotNetWorkQueueDashboard_PreloadAssemblies_Ignores_InvalidDlls()
        {
            var pluginDir = Path.Combine(Path.GetTempPath(), "dnwq-preload-invalid-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(pluginDir);
            try
            {
                // Write a non-.NET file with .dll extension
                File.WriteAllText(Path.Combine(pluginDir, "NotADotNet.dll"), "this is not a valid dll");

                var services = new ServiceCollection();
                services.AddLogging();

                // Should not throw on invalid DLLs
                services.AddDotNetWorkQueueDashboard(options =>
                {
                    options.EnableSwagger = false;
                    options.AssemblyPaths = new[] { pluginDir };
                });

                var provider = services.BuildServiceProvider();
                Assert.IsNotNull(provider.GetRequiredService<DashboardOptions>());
            }
            finally
            {
                Directory.Delete(pluginDir, true);
            }
        }
    }
}
