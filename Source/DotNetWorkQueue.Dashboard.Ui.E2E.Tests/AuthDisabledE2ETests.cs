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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DotNetWorkQueue.Dashboard.Ui.E2E.Tests.Fixtures;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Playwright.Assertions;

namespace DotNetWorkQueue.Dashboard.Ui.E2E.Tests
{
    [TestClass]
    public class AuthDisabledE2ETests : E2ETestBase
    {
        private static DashboardSubprocess _server = null!;

        protected override string BaseUrl => _server.RootUrl;

        [ClassInitialize]
        public static async Task ClassInit(TestContext context)
        {
            _server = await DashboardSubprocess.StartAsync(new Dictionary<string, string?>
            {
                // Auth is disabled when Username/PasswordHash are empty.
                ["DashboardAuth:Username"] = "",
                ["DashboardAuth:PasswordHash"] = ""
            });
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            _server?.Dispose();
        }

        [TestMethod]
        public async Task Root_DoesNotRedirectToLogin_WhenAuthDisabled()
        {
            await Page.GotoAsync("/");

            await Expect(Page).ToHaveURLAsync(new Regex("^(?!.*/login).*$"));
        }

        [TestMethod]
        public async Task HomeRendersAppBar_WhenAuthDisabled()
        {
            await Page.GotoAsync("/");

            await Expect(Page.GetByText("DotNetWorkQueue Dashboard")).ToBeVisibleAsync();
        }

        [TestMethod]
        public async Task DoesNotShowSignOutButton_WhenAuthDisabled()
        {
            await Page.GotoAsync("/");
            await Expect(Page.GetByText("DotNetWorkQueue Dashboard")).ToBeVisibleAsync();

            var signOut = Page.GetByTitle("Sign out");
            await Expect(signOut).ToHaveCountAsync(0);
        }
    }
}
