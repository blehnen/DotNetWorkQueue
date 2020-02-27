// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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

namespace DotNetWorkQueue
{
    /// <summary>
    /// Contains <see cref="CancellationTokenSource"/> for stopping or canceling processing
    /// <remarks>
    /// Generally speaking
    /// Stop == Do not process new work
    /// Cancel == Stop processing current work
    /// </remarks>
    /// </summary>
    public interface IQueueCancelWork : ICancelWork, IDisposable, IIsDisposed
    {
        /// <summary>
        /// Gets the cancellation token source.
        /// </summary>
        /// <remarks>
        /// This is used to tell both the queue/workers and user code to cancel the current operation
        /// </remarks>
        /// <value>
        /// The cancellation token source.
        /// </value>
        CancellationTokenSource CancellationTokenSource { get; }
        /// <summary>
        /// Gets the stop token source.
        /// </summary>
        /// <remarks>
        /// This is used to tell the queue/workers that they should no longer look for new messages to process
        /// </remarks>
        /// <value>
        /// The stop token source.
        /// </value>
        CancellationTokenSource StopTokenSource { get; }
    }
}
