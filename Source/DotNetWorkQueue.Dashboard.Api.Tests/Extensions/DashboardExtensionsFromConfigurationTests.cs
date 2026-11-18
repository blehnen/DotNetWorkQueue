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
using System.Globalization;
using System.Reflection;
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
        /// <summary>
        /// A configuration written before 0.16.0 has to be refused rather than ignored.
        /// </summary>
        /// <remarks>
        /// The options binder skips keys it does not recognise, so removing the TripleDes property
        /// on its own would leave the dashboard starting happily without the decryption it used to
        /// have. Every 3DES message would then fail to deserialize, with nothing pointing back at
        /// the setting that caused it (GitHub #183).
        /// </remarks>
        [TestMethod]
        public void AddDotNetWorkQueueDashboard_FromConfiguration_RefusesALeftoverTripleDesSection()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["Dashboard:Connections:0:Transport"] = "SQLite",
                    ["Dashboard:Connections:0:ConnectionString"] = "Data Source=:memory:",
                    ["Dashboard:Connections:0:Queues:0"] = "test-queue",
                    ["Dashboard:Interceptors:TripleDes:Key"] = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                    ["Dashboard:Interceptors:TripleDes:IV"] = "aaaaaaaaaaa="
                })
                .Build();

            var services = new ServiceCollection();

            var error = Assert.Throws<InvalidOperationException>(
                () => services.AddDotNetWorkQueueDashboard(config.GetSection("Dashboard")));

            //the message has to name the setting and say what to do, or it is no better than the
            //silence it replaces
            StringAssert.Contains(error.Message, "Interceptors:TripleDes");
            StringAssert.Contains(error.Message, "Aes");
        }

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

        /// <summary>
        /// Every public setting on <see cref="DashboardOptions"/> can be set from configuration.
        /// </summary>
        /// <remarks>
        /// The overload used to copy three settings by hand, so ReadOnly and six others were ignored and
        /// the Docker image could not run read-only (GitHub #438). This finds the settings by reflection,
        /// so one added later is covered without editing the test.
        /// </remarks>
        [TestMethod]
        public void AddDotNetWorkQueueDashboard_FromConfiguration_BindsEveryOption()
        {
            var defaults = new DashboardOptions();
            var values = new Dictionary<string, string>();
            var expected = new Dictionary<string, object>();
            foreach (var property in SettableOptions())
            {
                var value = NonDefaultValue(property, property.GetValue(defaults));
                expected[property.Name] = value;
                if (value is string[] array)
                {
                    for (var i = 0; i < array.Length; i++)
                        values[$"Dashboard:{property.Name}:{i}"] = array[i];
                }
                else
                {
                    values[$"Dashboard:{property.Name}"] = Convert.ToString(value, CultureInfo.InvariantCulture);
                }
            }

            var options = BuildFromConfiguration(values);

            var notBound = SettableOptions()
                .Where(p => expected[p.Name] is string[] array
                    ? !array.SequenceEqual((string[])p.GetValue(options))
                    : !Equals(expected[p.Name], p.GetValue(options)))
                .Select(p => p.Name)
                .ToList();
            Assert.IsEmpty(notBound, "Not bound from configuration: " + string.Join(", ", notBound));
        }

        [TestMethod]
        public void AddDotNetWorkQueueDashboard_FromConfiguration_AbsentSettingsKeepTheirDefaults()
        {
            var defaults = new DashboardOptions();

            var options = BuildFromConfiguration(new Dictionary<string, string>());

            foreach (var property in SettableOptions())
            {
                var expected = property.GetValue(defaults);
                if (expected is string[] array)
                    Assert.AreSequenceEqual(array, (string[])property.GetValue(options), property.Name);
                else
                    Assert.AreEqual(expected, property.GetValue(options), property.Name);
            }
        }

        private static IEnumerable<PropertyInfo> SettableOptions()
        {
            return typeof(DashboardOptions).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite && p.SetMethod!.IsPublic);
        }

        private static object NonDefaultValue(PropertyInfo property, object current)
        {
            if (property.PropertyType == typeof(bool))
                return !(bool)current;
            if (property.PropertyType == typeof(int))
                return (int)current + 7;
            if (property.PropertyType == typeof(string))
                return "configured-" + property.Name;
            if (property.PropertyType == typeof(string[]))
                return new[] { "first-" + property.Name, "second-" + property.Name };

            Assert.Fail($"DashboardOptions.{property.Name} is a {property.PropertyType.Name}, which this test " +
                        "cannot give a value yet. Add a case so the setting is checked.");
            return null;
        }

        private static DashboardOptions BuildFromConfiguration(Dictionary<string, string> values)
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddDotNetWorkQueueDashboard(config.GetSection("Dashboard"));
            return services.BuildServiceProvider().GetRequiredService<DashboardOptions>();
        }
    }
}
