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
using System.Security.Cryptography;
using System.Text;

namespace DotNetWorkQueue.Dashboard.Ui.E2E.Tests.Fixtures
{
    /// <summary>
    /// Fixed test credentials used when starting the Dashboard with authentication enabled.
    /// </summary>
    public static class DashboardAuthCredentials
    {
        public const string Username = "testuser";
        public const string Password = "testpass";

        public static string PasswordHash { get; } = ComputePasswordHash(Password);

        private static string ComputePasswordHash(string password)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (var b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}
