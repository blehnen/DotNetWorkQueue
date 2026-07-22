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
using DotNetWorkQueue.Dashboard.Ui.Components.Layout;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Dashboard.Ui.Tests.Components.Layout
{
    [TestClass]
    public class EmptyLayoutTests : BunitTestBase
    {
        [TestMethod]
        public void RendersBodyInsideDarkThemeContainer()
        {
            var cut = Render<EmptyLayout>(ps => ps
                .Add(p => p.Body, (RenderFragment)(builder =>
                {
                    builder.OpenElement(0, "span");
                    builder.AddContent(1, "layout-body-marker");
                    builder.CloseElement();
                })));

            StringAssert.Contains(cut.Markup, "layout-body-marker");
            StringAssert.Contains(cut.Markup, "mud-theme-dark");
        }
    }
}
