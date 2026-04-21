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
using System.Threading.Tasks;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Ui.E2E.Tests
{
    /// <summary>
    /// Assembly-level Playwright lifecycle. A single <see cref="IPlaywright"/>
    /// + headless Chromium <see cref="IBrowser"/> are created once per test run
    /// and shared across all test classes.
    /// </summary>
    [TestClass]
    public static class AssemblyFixture
    {
        public static IPlaywright Playwright { get; private set; } = null!;
        public static IBrowser Browser { get; private set; } = null!;

        [AssemblyInitialize]
        public static async Task Init(TestContext context)
        {
            Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true
            });
        }

        [AssemblyCleanup]
        public static async Task Cleanup()
        {
            if (Browser != null) await Browser.DisposeAsync();
            Playwright?.Dispose();
        }
    }
}
