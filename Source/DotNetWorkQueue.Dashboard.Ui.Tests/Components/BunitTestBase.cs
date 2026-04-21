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
using System.Security.Claims;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using MudBlazor.Services;

namespace DotNetWorkQueue.Dashboard.Ui.Tests.Components
{
    /// <summary>
    /// Base class for bUnit component tests. Registers MudBlazor services and
    /// enables loose JSInterop so MudBlazor components (which call into JS for
    /// ripples, popovers, etc.) can render without explicit interop mocks.
    /// </summary>
    public abstract class BunitTestBase : BunitContext
    {
        protected BunitTestBase()
        {
            Services.AddMudServices();
            JSInterop.Mode = JSRuntimeMode.Loose;
        }

        /// <summary>
        /// Registers a fake <see cref="AuthenticationStateProvider"/> that reports
        /// the requested authenticated state.
        /// </summary>
        protected void RegisterAuthState(bool authenticated, string userName = "test-user")
        {
            Services.AddSingleton<AuthenticationStateProvider>(new FakeAuthenticationStateProvider(authenticated, userName));
        }

        /// <summary>
        /// Renders a component wrapped with <see cref="MudPopoverProvider"/>, which
        /// MudBlazor components that use popovers (e.g. MudSelect) require to be
        /// present in the render tree. Attributes passed in are forwarded to the
        /// target component as (name, value) pairs.
        /// </summary>
        protected IRenderedComponent<IComponent> RenderWithMudProvider<TComponent>(params (string Name, object Value)[] attributes)
            where TComponent : IComponent
        {
            return Render(builder =>
            {
                builder.OpenComponent<MudPopoverProvider>(0);
                builder.CloseComponent();
                builder.OpenComponent<TComponent>(1);
#pragma warning disable ASP0006 // Deterministic per-call: attribute order is stable in test setup.
                var seq = 2;
                foreach (var attr in attributes)
                {
                    builder.AddAttribute(seq++, attr.Name, attr.Value);
                }
#pragma warning restore ASP0006
                builder.CloseComponent();
            });
        }

        private sealed class FakeAuthenticationStateProvider : AuthenticationStateProvider
        {
            private readonly bool _authenticated;
            private readonly string _userName;

            public FakeAuthenticationStateProvider(bool authenticated, string userName)
            {
                _authenticated = authenticated;
                _userName = userName;
            }

            public override Task<AuthenticationState> GetAuthenticationStateAsync()
            {
                var identity = _authenticated
                    ? new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, _userName) }, authenticationType: "test")
                    : new ClaimsIdentity();
                return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(identity)));
            }
        }
    }
}
