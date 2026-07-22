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
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using DotNetWorkQueue.Dashboard.Ui.Components.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MudBlazor;

namespace DotNetWorkQueue.Dashboard.Ui.Tests.Components.Shared
{
    [TestClass]
    public class AutoRefreshTests : BunitTestBase
    {
        [TestMethod]
        public void InitialRender_UsesDefaultIntervalSeconds()
        {
            var cut = RenderWithMudProvider<AutoRefresh>(
                (nameof(AutoRefresh.DefaultIntervalSeconds), 30));

            StringAssert.Contains(cut.Markup, "30s");
        }

        [TestMethod]
        public void TogglingOn_StartsTimer()
        {
            var refreshCount = 0;
            var callback = EventCallback.Factory.Create(this, () => refreshCount++);
            var cut = RenderWithMudProvider<AutoRefresh>(
                (nameof(AutoRefresh.OnRefresh), callback));

            var toggle = cut.FindComponent<MudToggleIconButton>();
            toggle.InvokeAsync(() => toggle.Instance.ToggledChanged.InvokeAsync(true));

            Assert.AreEqual(0, refreshCount);
        }

        [TestMethod]
        public void TogglingOff_StopsTimer()
        {
            var cut = RenderWithMudProvider<AutoRefresh>();

            var toggle = cut.FindComponent<MudToggleIconButton>();
            toggle.InvokeAsync(() => toggle.Instance.ToggledChanged.InvokeAsync(true));
            toggle.InvokeAsync(() => toggle.Instance.ToggledChanged.InvokeAsync(false));

            Assert.IsFalse(toggle.Instance.Toggled);
        }

        [TestMethod]
        public void ChangingInterval_WhileEnabled_RestartsTimer()
        {
            var cut = RenderWithMudProvider<AutoRefresh>();

            var toggle = cut.FindComponent<MudToggleIconButton>();
            toggle.InvokeAsync(() => toggle.Instance.ToggledChanged.InvokeAsync(true));

            var select = cut.FindComponent<MudSelect<int>>();
            select.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(60));

            StringAssert.Contains(cut.Markup, "60s");
        }

        [TestMethod]
        public void ChangingInterval_WhileDisabled_DoesNotStartTimer()
        {
            var cut = RenderWithMudProvider<AutoRefresh>();

            var select = cut.FindComponent<MudSelect<int>>();
            select.InvokeAsync(() => select.Instance.ValueChanged.InvokeAsync(60));

            var toggle = cut.FindComponent<MudToggleIconButton>();
            Assert.IsFalse(toggle.Instance.Toggled);
        }

        [TestMethod]
        public void Dispose_CalledTwice_IsSafe()
        {
            var cut = RenderWithMudProvider<AutoRefresh>();

            var toggle = cut.FindComponent<MudToggleIconButton>();
            toggle.InvokeAsync(() => toggle.Instance.ToggledChanged.InvokeAsync(true));

            var instance = cut.FindComponent<AutoRefresh>().Instance;
            ((IDisposable)instance).Dispose();
            ((IDisposable)instance).Dispose();
        }
    }
}
