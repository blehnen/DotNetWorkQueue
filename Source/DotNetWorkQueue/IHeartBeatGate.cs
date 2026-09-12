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

namespace DotNetWorkQueue
{
    /// <summary>
    /// Bounds how many heartbeat updates a consumer has in flight at once.
    /// </summary>
    public interface IHeartBeatGate : IDisposable
    {
        /// <summary>
        /// Waits up to <paramref name="maxWait"/> for a slot. Dispose the result to give it back.
        /// </summary>
        /// <param name="maxWait">How long to wait before going ahead without a slot.</param>
        /// <param name="cancellation">Cancels the wait.</param>
        /// <remarks>
        /// Always returns something to dispose. The bound is a throttle, not a gate on correctness: a
        /// beat that waited too long goes ahead anyway, because missing the beat costs the message its
        /// claim while exceeding a concurrency target costs a little database load.
        /// </remarks>
        Task<IDisposable> EnterAsync(TimeSpan maxWait, CancellationToken cancellation);
    }
}
