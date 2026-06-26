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
namespace DotNetWorkQueue.Transport.Shared.Basic
{
    /// <summary>
    /// Indicates whether the current transport provides a true batch-send (bulk insert) handler.
    /// <c>SendMessages&lt;T&gt;</c> uses this to decide whether to dispatch a batch send or fall
    /// back to the per-message loop.
    /// </summary>
    /// <remarks>
    /// This is a plain capability flag rather than a type check on the injected batch handler:
    /// command handlers are wrapped by open-generic decorators (retry, trace, metrics), so the
    /// injected handler reference is a decorator that hides any marker on the underlying no-op.
    /// A dedicated, non-decorated service makes the decision reliable.
    /// </remarks>
    public interface ISendMessageBatchSupport
    {
        /// <summary>
        /// <c>true</c> when the transport registered a real batch handler; <c>false</c> when batch
        /// sends must use the per-message loop.
        /// </summary>
        bool IsSupported { get; }
    }
}
