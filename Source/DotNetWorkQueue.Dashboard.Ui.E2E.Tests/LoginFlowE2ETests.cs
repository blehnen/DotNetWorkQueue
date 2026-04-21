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
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Microsoft.Playwright.Assertions;

namespace DotNetWorkQueue.Dashboard.Ui.E2E.Tests
{
    [TestClass]
    public class LoginFlowE2ETests : E2ETestBase
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
        public async Task PostingInvalidCredentials_RedirectsBackToLoginWithError()
        {
            await SubmitLoginAsync(DashboardAuthCredentials.Username, "wrong-password");

            await Expect(Page).ToHaveURLAsync(new Regex(".*/login.*error.*"));
            await Expect(Page.GetByText("Invalid username or password.")).ToBeVisibleAsync();
        }

        [TestMethod]
        public async Task PostingValidCredentials_NavigatesAwayFromLogin()
        {
            await SubmitLoginAsync(DashboardAuthCredentials.Username, DashboardAuthCredentials.Password);

            await Expect(Page).ToHaveURLAsync(new Regex("^(?!.*/login).*$"));
        }

        /// <summary>
        /// Posts a login form to <c>/auth/login</c> the same way the dashboard's
        /// inline <c>dashboardSubmitLogin</c> JavaScript helper does (in
        /// <c>App.razor</c>). This bypasses the MudButton OnClick handler — that
        /// path is brittle in tests because Playwright can race the Blazor
        /// SignalR circuit attaching.
        /// </summary>
        private async Task SubmitLoginAsync(string username, string password)
        {
            await Page.GotoAsync("/login");
            await Page.WaitForLoadStateAsync(LoadState.Load);

            await Page.EvaluateAsync(@"
                ({ username, password }) => {
                    const f = document.createElement('form');
                    f.method = 'POST';
                    f.action = '/auth/login';
                    const u = document.createElement('input');
                    u.type = 'hidden'; u.name = 'username'; u.value = username;
                    f.appendChild(u);
                    const p = document.createElement('input');
                    p.type = 'hidden'; p.name = 'password'; p.value = password;
                    f.appendChild(p);
                    document.body.appendChild(f);
                    f.submit();
                }",
                new { username, password });

            await Page.WaitForLoadStateAsync(LoadState.Load);
        }
    }
}
