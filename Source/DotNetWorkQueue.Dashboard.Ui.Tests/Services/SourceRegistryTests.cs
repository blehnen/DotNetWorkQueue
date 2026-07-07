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
using System.Collections.Generic;
using DotNetWorkQueue.Dashboard.Ui.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Ui.Tests.Services
{
    [TestClass]
    public class SourceRegistryTests
    {
        private static DashboardApiSourceConfig CreateSource(string name, string baseUrl = "https://example.com")
        {
            return new DashboardApiSourceConfig { Name = name, BaseUrl = baseUrl };
        }

        [TestMethod]
        public void GetAll_Returns_All_Sources()
        {
            var sources = new List<DashboardApiSourceConfig>
            {
                CreateSource("Local"),
                CreateSource("Production")
            };
            var registry = new SourceRegistry(sources);

            var result = registry.GetAll();

            Assert.HasCount(2, result);
        }

        [TestMethod]
        public void GetBySlug_Returns_Correct_Source()
        {
            var sources = new List<DashboardApiSourceConfig>
            {
                CreateSource("Local", "https://localhost:5001"),
                CreateSource("Production", "https://prod.example.com")
            };
            var registry = new SourceRegistry(sources);

            var result = registry.GetBySlug("local");

            Assert.IsNotNull(result);
            Assert.AreEqual("Local", result!.Name);
        }

        [TestMethod]
        public void GetBySlug_Returns_Null_For_Unknown()
        {
            var sources = new List<DashboardApiSourceConfig>
            {
                CreateSource("Local")
            };
            var registry = new SourceRegistry(sources);

            var result = registry.GetBySlug("nonexistent");

            Assert.IsNull(result);
        }

        [TestMethod]
        public void GetByName_Returns_Correct_Source()
        {
            var sources = new List<DashboardApiSourceConfig>
            {
                CreateSource("Local", "https://localhost:5001"),
                CreateSource("Production", "https://prod.example.com")
            };
            var registry = new SourceRegistry(sources);

            var result = registry.GetByName("Local");

            Assert.IsNotNull(result);
            Assert.AreEqual("https://localhost:5001", result!.BaseUrl);
        }

        [TestMethod]
        public void GetByName_Returns_Null_For_Unknown()
        {
            var sources = new List<DashboardApiSourceConfig>
            {
                CreateSource("Local")
            };
            var registry = new SourceRegistry(sources);

            var result = registry.GetByName("nonexistent");

            Assert.IsNull(result);
        }

        [TestMethod]
        public void Constructor_Throws_On_Empty_Sources()
        {
            var sources = new List<DashboardApiSourceConfig>();

            var act = () => new SourceRegistry(sources);

            var ex = Assert.Throws<ArgumentException>(act);
            StringAssert.Contains(ex.Message, "at least one", StringComparison.OrdinalIgnoreCase);
        }

        [TestMethod]
        public void Constructor_Throws_On_Null_Sources()
        {
            var act = () => new SourceRegistry(null!);

            Assert.Throws<ArgumentNullException>(act);
        }

        [TestMethod]
        public void Constructor_Throws_On_Duplicate_Names()
        {
            var sources = new List<DashboardApiSourceConfig>
            {
                CreateSource("Local"),
                CreateSource("Local")
            };

            var act = () => new SourceRegistry(sources);

            var ex = Assert.Throws<ArgumentException>(act);
            StringAssert.Contains(ex.Message, "duplicate", StringComparison.OrdinalIgnoreCase);
            StringAssert.Contains(ex.Message, "Local", StringComparison.OrdinalIgnoreCase);
        }

        [TestMethod]
        public void Constructor_Throws_On_Duplicate_Slugs()
        {
            var sources = new List<DashboardApiSourceConfig>
            {
                CreateSource("My Server"),
                CreateSource("my--server")
            };

            var act = () => new SourceRegistry(sources);

            var ex = Assert.Throws<ArgumentException>(act);
            StringAssert.Contains(ex.Message, "slug", StringComparison.OrdinalIgnoreCase);
        }

        [TestMethod]
        public void GetAll_Returns_ReadOnly_Collection()
        {
            var sources = new List<DashboardApiSourceConfig>
            {
                CreateSource("Local")
            };
            var registry = new SourceRegistry(sources);

            var result = registry.GetAll();

            Assert.IsInstanceOfType(result, typeof(IReadOnlyList<DashboardApiSourceConfig>));
        }

        [TestMethod]
        public void GetByName_Is_Case_Insensitive()
        {
            var sources = new List<DashboardApiSourceConfig>
            {
                CreateSource("Local", "https://localhost:5001")
            };
            var registry = new SourceRegistry(sources);

            var result = registry.GetByName("LOCAL");

            Assert.IsNotNull(result);
            Assert.AreEqual("Local", result!.Name);
        }
    }
}
