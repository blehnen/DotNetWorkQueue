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
    /// Marker implemented by the no-op batch-send handler that is registered as a fallback
    /// for transports that do not provide a true bulk-insert path. <c>SendMessages&lt;T&gt;</c>
    /// checks for this marker to decide whether to dispatch a batch send or fall back to the
    /// per-message loop.
    /// </summary>
    /// <remarks>
    /// Using a marker rather than a null return keeps the per-message fallback decision
    /// explicit and total: a real batch handler never carries this marker, so its presence
    /// unambiguously means "no batch handler was registered for this transport."
    /// </remarks>
    public interface ISendMessageBatchNotSupported
    {
    }
}
