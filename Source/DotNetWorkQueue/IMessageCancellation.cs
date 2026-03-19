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
using System.Threading;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Provides per-message cancellation support. The token is created when a message
    /// begins processing and canceled when a cancel request is received via the dashboard
    /// or programmatically. The user's handler should check this token to support
    /// cooperative cancellation.
    /// </summary>
    public interface IMessageCancellation
    {
        /// <summary>
        /// A cancellation token specific to the current message being processed.
        /// This token is linked with the worker-level cancellation tokens, so it fires
        /// when either the worker is stopping OR a per-message cancel is requested.
        /// </summary>
        CancellationToken Token { get; }
    }
}
