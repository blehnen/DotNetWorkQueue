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
using FluentAssertions;
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

            result.Should().HaveCount(2);
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

            result.Should().NotBeNull();
            result!.Name.Should().Be("Local");
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

            result.Should().BeNull();
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

            result.Should().NotBeNull();
            result!.BaseUrl.Should().Be("https://localhost:5001");
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

            result.Should().BeNull();
        }

        [TestMethod]
        public void Constructor_Throws_On_Empty_Sources()
        {
            var sources = new List<DashboardApiSourceConfig>();

            var act = () => new SourceRegistry(sources);

            act.Should().Throw<ArgumentException>()
                .WithMessage("*at least one*");
        }

        [TestMethod]
        public void Constructor_Throws_On_Null_Sources()
        {
            var act = () => new SourceRegistry(null!);

            act.Should().Throw<ArgumentNullException>();
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

            act.Should().Throw<ArgumentException>()
                .WithMessage("*duplicate*")
                .WithMessage("*Local*");
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

            act.Should().Throw<ArgumentException>()
                .WithMessage("*slug*");
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

            result.Should().BeAssignableTo<IReadOnlyList<DashboardApiSourceConfig>>();
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

            result.Should().NotBeNull();
            result!.Name.Should().Be("Local");
        }
    }
}
