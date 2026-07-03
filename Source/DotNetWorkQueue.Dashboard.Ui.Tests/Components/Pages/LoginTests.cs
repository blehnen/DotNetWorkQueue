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
using DotNetWorkQueue.Dashboard.Ui.Components.Pages;
using DotNetWorkQueue.Dashboard.Ui.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Ui.Tests.Components.Pages
{
    [TestClass]
    public class LoginTests : BunitTestBase
    {
        [TestMethod]
        public void RendersSignInButton_WhenAuthEnabled()
        {
            Services.AddSingleton(new DashboardAuthConfig { IsEnabled = true });

            var cut = Render<Login>();

            StringAssert.Contains(cut.Markup, "Sign In");
            StringAssert.Contains(cut.Markup, "Dashboard Login");
        }

        [TestMethod]
        public void ShowsErrorAlert_WhenErrorQueryParamPresent()
        {
            Services.AddSingleton(new DashboardAuthConfig { IsEnabled = true });
            var nav = Services.GetRequiredService<BunitNavigationManager>();
            nav.NavigateTo("/login?error=1");

            var cut = Render<Login>();

            StringAssert.Contains(cut.Markup, "Invalid username or password.");
        }

        [TestMethod]
        public void DoesNotShowErrorAlert_WhenNoErrorQueryParam()
        {
            Services.AddSingleton(new DashboardAuthConfig { IsEnabled = true });

            var cut = Render<Login>();

            Assert.IsFalse(cut.Markup.Contains("Invalid username or password."));
        }

        [TestMethod]
        public void NavigatesToRoot_WhenAuthDisabled()
        {
            Services.AddSingleton(new DashboardAuthConfig { IsEnabled = false });
            var nav = Services.GetRequiredService<BunitNavigationManager>();

            Render<Login>();

            Assert.AreEqual(nav.BaseUri, nav.Uri);
        }
    }
}
