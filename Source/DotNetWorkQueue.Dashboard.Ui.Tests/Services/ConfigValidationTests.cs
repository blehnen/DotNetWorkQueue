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

#nullable enable

using System;
using System.Collections.Generic;
using DotNetWorkQueue.Dashboard.Ui.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Ui.Tests.Services
{
    [TestClass]
    public class ConfigValidationTests
    {
        private static global::Microsoft.Extensions.Configuration.IConfiguration BuildConfig(Dictionary<string, string?> values)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(values)
                .Build();
        }

        [TestMethod]
        public void ParseSources_Returns_Sources_From_Config()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                { "DashboardApi:Sources:0:Name", "Local" },
                { "DashboardApi:Sources:0:BaseUrl", "http://localhost:5000" }
            });

            var result = DashboardConfigParser.ParseSources(config);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("Local", result[0].Name);
            Assert.AreEqual("http://localhost:5000", result[0].BaseUrl);
        }

        [TestMethod]
        public void ParseSources_Returns_Multiple_Sources()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                { "DashboardApi:Sources:0:Name", "Local" },
                { "DashboardApi:Sources:0:BaseUrl", "http://localhost:5000" },
                { "DashboardApi:Sources:1:Name", "Production" },
                { "DashboardApi:Sources:1:BaseUrl", "https://prod.example.com" }
            });

            var result = DashboardConfigParser.ParseSources(config);

            Assert.AreEqual(2, result.Count);
        }

        [TestMethod]
        public void ParseSources_With_ApiKey()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                { "DashboardApi:Sources:0:Name", "Local" },
                { "DashboardApi:Sources:0:BaseUrl", "http://localhost:5000" },
                { "DashboardApi:Sources:0:ApiKey", "my-secret-key" }
            });

            var result = DashboardConfigParser.ParseSources(config);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("my-secret-key", result[0].ApiKey);
        }

        [TestMethod]
        public void ParseSources_Without_ApiKey()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                { "DashboardApi:Sources:0:Name", "Local" },
                { "DashboardApi:Sources:0:BaseUrl", "http://localhost:5000" }
            });

            var result = DashboardConfigParser.ParseSources(config);

            Assert.AreEqual(1, result.Count);
            Assert.IsNull(result[0].ApiKey);
        }

        [TestMethod]
        public void ValidateNoLegacyConfig_Throws_On_Old_Format()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                { "DashboardApi:BaseUrl", "http://localhost:5000" }
            });

            var act = () => DashboardConfigParser.ValidateNoLegacyConfig(config);

            var ex = Assert.Throws<InvalidOperationException>(act);
            StringAssert.Contains(ex.Message, "Sources", StringComparison.OrdinalIgnoreCase);
            StringAssert.Contains(ex.Message, "migration", StringComparison.OrdinalIgnoreCase);
        }

        [TestMethod]
        public void ValidateNoLegacyConfig_Does_Not_Throw_When_Sources_Present()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                { "DashboardApi:BaseUrl", "http://localhost:5000" },
                { "DashboardApi:Sources:0:Name", "Local" },
                { "DashboardApi:Sources:0:BaseUrl", "http://localhost:5000" }
            });

            var act = () => DashboardConfigParser.ValidateNoLegacyConfig(config);

            act();
        }

        [TestMethod]
        public void ValidateNoLegacyConfig_Does_Not_Throw_When_Neither_Present()
        {
            var config = BuildConfig(new Dictionary<string, string?>
            {
                { "SomeOther:Key", "value" }
            });

            var act = () => DashboardConfigParser.ValidateNoLegacyConfig(config);

            act();
        }
    }
}
