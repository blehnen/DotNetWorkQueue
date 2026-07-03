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
            Assert.AreEqual("local", config.Slug);
        }

        [TestMethod]
        public void Slug_From_Name_With_Spaces()
        {
            var config = new DashboardApiSourceConfig { Name = "Production SQL Server" };
            Assert.AreEqual("production-sql-server", config.Slug);
        }

        [TestMethod]
        public void Slug_From_Name_With_Special_Chars()
        {
            var config = new DashboardApiSourceConfig { Name = "My Server (US-East)" };
            Assert.AreEqual("my-server-us-east", config.Slug);
        }

        [TestMethod]
        public void Slug_Collapses_Consecutive_Hyphens()
        {
            var config = new DashboardApiSourceConfig { Name = "test--name" };
            Assert.AreEqual("test-name", config.Slug);
        }

        [TestMethod]
        public void Slug_Trims_Leading_Trailing_Hyphens()
        {
            var config = new DashboardApiSourceConfig { Name = " -Test- " };
            Assert.AreEqual("test", config.Slug);
        }

        [TestMethod]
        public void Slug_From_Name_With_Numbers()
        {
            var config = new DashboardApiSourceConfig { Name = "Server 42" };
            Assert.AreEqual("server-42", config.Slug);
        }

        [TestMethod]
        public void Name_Set_Get()
        {
            var config = new DashboardApiSourceConfig { Name = "MySource" };
            Assert.AreEqual("MySource", config.Name);
        }

        [TestMethod]
        public void BaseUrl_Set_Get()
        {
            var config = new DashboardApiSourceConfig { BaseUrl = "https://example.com/api" };
            Assert.AreEqual("https://example.com/api", config.BaseUrl);
        }

        [TestMethod]
        public void ApiKey_Set_Get()
        {
            var config = new DashboardApiSourceConfig { ApiKey = "secret-key-123" };
            Assert.AreEqual("secret-key-123", config.ApiKey);
        }

        [TestMethod]
        public void ApiKey_Defaults_To_Null()
        {
            var config = new DashboardApiSourceConfig();
            Assert.IsNull(config.ApiKey);
        }
    }
}
