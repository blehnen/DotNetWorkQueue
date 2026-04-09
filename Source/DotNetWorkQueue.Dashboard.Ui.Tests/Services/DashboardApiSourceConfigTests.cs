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

using DotNetWorkQueue.Dashboard.Ui.Services;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Ui.Tests.Services
{
    [TestClass]
    public class DashboardApiSourceConfigTests
    {
        [TestMethod]
        public void Slug_From_Simple_Name()
        {
            var config = new DashboardApiSourceConfig { Name = "Local" };
            config.Slug.Should().Be("local");
        }

        [TestMethod]
        public void Slug_From_Name_With_Spaces()
        {
            var config = new DashboardApiSourceConfig { Name = "Production SQL Server" };
            config.Slug.Should().Be("production-sql-server");
        }

        [TestMethod]
        public void Slug_From_Name_With_Special_Chars()
        {
            var config = new DashboardApiSourceConfig { Name = "My Server (US-East)" };
            config.Slug.Should().Be("my-server-us-east");
        }

        [TestMethod]
        public void Slug_Collapses_Consecutive_Hyphens()
        {
            var config = new DashboardApiSourceConfig { Name = "test--name" };
            config.Slug.Should().Be("test-name");
        }

        [TestMethod]
        public void Slug_Trims_Leading_Trailing_Hyphens()
        {
            var config = new DashboardApiSourceConfig { Name = " -Test- " };
            config.Slug.Should().Be("test");
        }

        [TestMethod]
        public void Slug_From_Name_With_Numbers()
        {
            var config = new DashboardApiSourceConfig { Name = "Server 42" };
            config.Slug.Should().Be("server-42");
        }

        [TestMethod]
        public void Name_Set_Get()
        {
            var config = new DashboardApiSourceConfig { Name = "MySource" };
            config.Name.Should().Be("MySource");
        }

        [TestMethod]
        public void BaseUrl_Set_Get()
        {
            var config = new DashboardApiSourceConfig { BaseUrl = "https://example.com/api" };
            config.BaseUrl.Should().Be("https://example.com/api");
        }

        [TestMethod]
        public void ApiKey_Set_Get()
        {
            var config = new DashboardApiSourceConfig { ApiKey = "secret-key-123" };
            config.ApiKey.Should().Be("secret-key-123");
        }

        [TestMethod]
        public void ApiKey_Defaults_To_Null()
        {
            var config = new DashboardApiSourceConfig();
            config.ApiKey.Should().BeNull();
        }
    }
}
