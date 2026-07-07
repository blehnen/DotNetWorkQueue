// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
//
//This library is free software; you can redistribute it and/or
//modify it under the terms of the GNU Lesser General Public
//License as published by the Free Software Foundation; either
//version 2.1 of the License, or (at your option) any later version.
//
//This library is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
//Lesser General Public License for more details.
//
//You should have received a copy of the GNU Lesser General Public
//License along with this library; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
// ---------------------------------------------------------------------
using System;
using System.Linq;
using System.Collections.Generic;
using DotNetWorkQueue.Dashboard.Api;
using DotNetWorkQueue.Dashboard.Api.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Api.Tests.Extensions
{
    [TestClass]
    public class DashboardExtensionsFromConfigurationTests
    {
        // Task 1: Happy-path test using SQLite :memory: connection string
        [TestMethod]
        public void AddDotNetWorkQueueDashboard_FromConfiguration_Memory_RegistersConnection()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Dashboard:EnableSwagger"] = "false",
                    ["Dashboard:Connections:0:Transport"] = "SQLite",
                    ["Dashboard:Connections:0:ConnectionString"] = "Data Source=:memory:",
                    ["Dashboard:Connections:0:DisplayName"] = "TestDb",
                    ["Dashboard:Connections:0:Queues:0"] = "test-queue"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDotNetWorkQueueDashboard(config.GetSection("Dashboard"));

            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<DashboardOptions>();
            Assert.IsFalse(options.EnableSwagger);
            Assert.IsNotEmpty(options.ConnectionRegistrations);
        }

        // Task 2: Parameterized test over all 5 valid transport names
        [TestMethod]
        [DataRow("SqlServer", "Server=localhost;Database=Test;Integrated Security=true")]
        [DataRow("PostgreSql", "Host=localhost;Database=test;Username=test")]
        [DataRow("SQLite", "Data Source=:memory:")]
        [DataRow("LiteDb", "Filename=:memory:")]
        [DataRow("Redis", "localhost:6379")]
        public void AddDotNetWorkQueueDashboard_FromConfiguration_AllTransports_RegisterCleanly(
            string transport, string connectionString)
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Dashboard:EnableSwagger"] = "false",
                    ["Dashboard:Connections:0:Transport"] = transport,
                    ["Dashboard:Connections:0:ConnectionString"] = connectionString
                })
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();

            // Must not throw for any valid transport name
            services.AddDotNetWorkQueueDashboard(config.GetSection("Dashboard"));

            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<DashboardOptions>();
            Assert.IsNotEmpty(options.ConnectionRegistrations);
        }

        // Task 2: Unknown-transport error test
        [TestMethod]
        public void AddDotNetWorkQueueDashboard_FromConfiguration_UnknownTransport_Throws()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Dashboard:Connections:0:Transport"] = "MongoDB",
                    ["Dashboard:Connections:0:ConnectionString"] = "mongodb://localhost"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();

            Assert.ThrowsExactly<ArgumentException>(() =>
                services.AddDotNetWorkQueueDashboard(config.GetSection("Dashboard")));
        }

        // Task 3: Missing-Transport error test
        [TestMethod]
        public void AddDotNetWorkQueueDashboard_FromConfiguration_MissingTransport_Throws()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    // Transport missing
                    ["Dashboard:Connections:0:ConnectionString"] = "Data Source=:memory:"
                })
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();

            var ex = Assert.ThrowsExactly<ArgumentException>(() =>
                services.AddDotNetWorkQueueDashboard(config.GetSection("Dashboard")));

            StringAssert.Contains(ex.Message, "Transport");
        }

        // Task 3: Missing-ConnectionString error test
        [TestMethod]
        public void AddDotNetWorkQueueDashboard_FromConfiguration_MissingConnectionString_Throws()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Dashboard:Connections:0:Transport"] = "SQLite",
                    ["Dashboard:Connections:0:DisplayName"] = "Broken"
                    // ConnectionString missing
                })
                .Build();

            var services = new ServiceCollection();
            services.AddLogging();

            var ex = Assert.ThrowsExactly<ArgumentException>(() =>
                services.AddDotNetWorkQueueDashboard(config.GetSection("Dashboard")));

            StringAssert.Contains(ex.Message, "ConnectionString");
        }
    }
}
