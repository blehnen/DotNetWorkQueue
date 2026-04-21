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
    /// Per-test Playwright context + page. Shares the assembly-wide browser
    /// from <see cref="AssemblyFixture"/>. Each test gets a fresh isolated
    /// browser context.
    /// </summary>
    public abstract class E2ETestBase
    {
        protected IBrowserContext Context { get; private set; } = null!;
        protected IPage Page { get; private set; } = null!;

        /// <summary>Override to supply the <c>BaseURL</c> for this test class.</summary>
        protected abstract string BaseUrl { get; }

        [TestInitialize]
        public async Task InitContext()
        {
            Context = await AssemblyFixture.Browser.NewContextAsync(new BrowserNewContextOptions
            {
                BaseURL = BaseUrl,
                IgnoreHTTPSErrors = true
            });
            Page = await Context.NewPageAsync();
        }

        [TestCleanup]
        public async Task DisposeContext()
        {
            await Context.CloseAsync();
        }
    }
}
