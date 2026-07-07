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
using Bunit;
using Bunit.TestDoubles;
using DotNetWorkQueue.Dashboard.Ui.Components.Layout;
using DotNetWorkQueue.Dashboard.Ui.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Ui.Tests.Components.Layout
{
    [TestClass]
    public class MainLayoutTests : BunitTestBase
    {
        [TestMethod]
        public void RendersLayout_WhenAuthDisabled()
        {
            Services.AddSingleton(new DashboardAuthConfig { IsEnabled = false });
            RegisterAuthState(authenticated: false);

            var cut = Render<MainLayout>();

            StringAssert.Contains(cut.Markup, "DotNetWorkQueue Dashboard");
        }

        [TestMethod]
        public void RendersLayout_WhenAuthEnabledAndAuthenticated()
        {
            Services.AddSingleton(new DashboardAuthConfig { IsEnabled = true });
            RegisterAuthState(authenticated: true);

            var cut = Render<MainLayout>();

            StringAssert.Contains(cut.Markup, "DotNetWorkQueue Dashboard");
        }

        [TestMethod]
        public void ShowsLogoutButton_WhenAuthEnabled()
        {
            Services.AddSingleton(new DashboardAuthConfig { IsEnabled = true });
            RegisterAuthState(authenticated: true);

            var cut = Render<MainLayout>();

            StringAssert.Contains(cut.Markup, "Sign out");
        }

        [TestMethod]
        public void DoesNotShowLogoutButton_WhenAuthDisabled()
        {
            Services.AddSingleton(new DashboardAuthConfig { IsEnabled = false });
            RegisterAuthState(authenticated: false);

            var cut = Render<MainLayout>();

            Assert.DoesNotContain("Sign out", cut.Markup);
        }

        [TestMethod]
        public void NavigatesToLogin_WhenAuthEnabledAndNotAuthenticated()
        {
            Services.AddSingleton(new DashboardAuthConfig { IsEnabled = true });
            RegisterAuthState(authenticated: false);
            var nav = Services.GetRequiredService<BunitNavigationManager>();

            Render<MainLayout>();

            StringAssert.EndsWith(nav.Uri, "/login");
        }
    }
}
