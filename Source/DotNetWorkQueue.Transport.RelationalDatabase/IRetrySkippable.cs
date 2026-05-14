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
namespace DotNetWorkQueue.Transport.RelationalDatabase
{
    /// <summary>
    /// Marker interface for command objects that opt out of the relational retry decorator
    /// on a per-call basis. The retry decorator inspects this property at <c>Handle()</c>
    /// time and invokes the inner handler directly when <see cref="SkipRetry"/> is
    /// <c>true</c>.
    /// </summary>
    /// <remarks>
    /// Introduced by the outbox-pattern feature so caller-supplied-transaction sends bypass
    /// the Polly retry pipeline (the caller owns retry semantics on this path). Implemented
    /// by <c>RelationalSendMessageCommand</c>.
    /// </remarks>
    public interface IRetrySkippable
    {
        /// <summary>
        /// Gets a value indicating whether the retry decorator should skip its Polly
        /// pipeline and invoke the inner handler directly for this command instance.
        /// </summary>
        bool SkipRetry { get; }
    }
}
