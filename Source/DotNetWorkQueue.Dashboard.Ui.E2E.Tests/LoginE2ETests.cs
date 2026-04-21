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
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetWorkQueue.Dashboard.Ui.E2E.Tests.Fixtures;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Playwright.Assertions;

namespace DotNetWorkQueue.Dashboard.Ui.E2E.Tests
{
    [TestClass]
    public class LoginE2ETests : E2ETestBase
    {
        private static DashboardSubprocess _server = null!;

        protected override string BaseUrl => _server.RootUrl;

        [ClassInitialize]
        public static async Task ClassInit(TestContext context)
        {
            _server = await DashboardSubprocess.StartAsync(new Dictionary<string, string?>
            {
                ["DashboardAuth:Username"] = DashboardAuthCredentials.Username,
                ["DashboardAuth:PasswordHash"] = DashboardAuthCredentials.PasswordHash
            });
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            _server?.Dispose();
        }

        [TestMethod]
        public async Task LoginPageRenders_WithUsernameAndPasswordFields()
        {
            await Page.GotoAsync("/login");

            await Expect(Page.GetByLabel("Username")).ToBeVisibleAsync();
            await Expect(Page.GetByLabel("Password")).ToBeVisibleAsync();
            await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Sign In" })).ToBeVisibleAsync();
        }
    }
}
