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
namespace DotNetWorkQueue.Dashboard.Ui.Services
{
    /// <summary>
    /// Configuration for simple dashboard authentication.
    /// </summary>
    public class DashboardAuthConfig
    {
        /// <summary>
        /// Whether authentication is enabled.
        /// </summary>
        public bool IsEnabled { get; set; }

        /// <summary>
        /// The expected username.
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// The SHA256 hex hash of the expected password.
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;
    }
}
