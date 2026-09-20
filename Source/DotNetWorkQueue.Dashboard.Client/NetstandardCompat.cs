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
#if NETSTANDARD2_0
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetWorkQueue.Dashboard.Client
{
    /// <summary>
    /// Framework methods netstandard2.0 does not have, under the names the rest of this package
    /// already calls them by.
    /// </summary>
    /// <remarks>
    /// Local to this package rather than shared: it is the only one that needs these, and the
    /// shared compatibility file is linked into every project (GitHub #401).
    /// </remarks>
    internal static class NetstandardCompat
    {
        /// <summary>
        /// Stands in for the <c>ReadAsStringAsync(CancellationToken)</c> overload added in .NET 5.
        /// </summary>
        /// <remarks>
        /// The token is accepted and not used. Every caller here reads a buffered response, which
        /// HttpClient has already finished receiving under the token passed to SendAsync, so there
        /// is no outstanding network wait left for it to cancel.
        /// </remarks>
        public static Task<string> ReadAsStringAsync(this HttpContent content, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return content.ReadAsStringAsync();
        }

        /// <summary>
        /// Stands in for <c>Timer.DisposeAsync</c>, added in .NET Core 3.0.
        /// </summary>
        /// <remarks>
        /// The asynchronous form waits for a callback that is already running; this does not. The
        /// caller has already stopped the timer and awaited its work before disposing it.
        /// </remarks>
        public static ValueTask DisposeAsync(this Timer timer)
        {
            timer.Dispose();
            return default;
        }
    }
}
#endif
