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
using System.Linq;
using Bunit;
using Bunit.TestDoubles;
using DotNetWorkQueue.Dashboard.Ui.Components.Pages;
using DotNetWorkQueue.Dashboard.Ui.Services;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MudBlazor;

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

            Assert.DoesNotContain("Invalid username or password.", cut.Markup);
        }

        [TestMethod]
        public void NavigatesToRoot_WhenAuthDisabled()
        {
            Services.AddSingleton(new DashboardAuthConfig { IsEnabled = false });
            var nav = Services.GetRequiredService<BunitNavigationManager>();

            Render<Login>();

            Assert.AreEqual(nav.BaseUri, nav.Uri);
        }

        [TestMethod]
        public void SubmitButton_PostsCredentials_WhenBothFieldsFilled()
        {
            Services.AddSingleton(new DashboardAuthConfig { IsEnabled = true });
            var invocation = JSInterop.SetupVoid("dashboardSubmitLogin", "/auth/login", "alice", "s3cret");

            var cut = Render<Login>();
            SetCredentials(cut, "alice", "s3cret");
            cut.Find("button").Click();

            Assert.HasCount(1, invocation.Invocations);
        }

        [TestMethod]
        public void SubmitButton_DoesNothing_WhenCredentialsIncomplete()
        {
            Services.AddSingleton(new DashboardAuthConfig { IsEnabled = true });
            var invocation = JSInterop.SetupVoid("dashboardSubmitLogin", _ => true);

            var cut = Render<Login>();
            SetCredentials(cut, "alice", "   ");
            cut.Find("button").Click();

            Assert.HasCount(0, invocation.Invocations);
        }

        [TestMethod]
        public void EnterKey_SubmitsCredentials()
        {
            Services.AddSingleton(new DashboardAuthConfig { IsEnabled = true });
            var invocation = JSInterop.SetupVoid("dashboardSubmitLogin", "/auth/login", "bob", "hunter2");

            var cut = Render<Login>();
            SetCredentials(cut, "bob", "hunter2");
            cut.FindAll("input")[0].KeyDown(new KeyboardEventArgs { Key = "Enter" });

            Assert.HasCount(1, invocation.Invocations);
        }

        [TestMethod]
        public void NonEnterKey_DoesNotSubmit()
        {
            Services.AddSingleton(new DashboardAuthConfig { IsEnabled = true });
            var invocation = JSInterop.SetupVoid("dashboardSubmitLogin", _ => true);

            var cut = Render<Login>();
            SetCredentials(cut, "bob", "hunter2");
            cut.FindAll("input")[0].KeyDown(new KeyboardEventArgs { Key = "a" });

            Assert.HasCount(0, invocation.Invocations);
        }

        private static void SetCredentials(IRenderedComponent<Login> cut, string username, string password)
        {
            var fields = cut.FindComponents<MudTextField<string>>();
            cut.InvokeAsync(() => fields[0].Instance.ValueChanged.InvokeAsync(username));
            cut.InvokeAsync(() => fields[1].Instance.ValueChanged.InvokeAsync(password));
        }
    }
}
