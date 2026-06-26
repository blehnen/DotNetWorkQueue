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

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    /// <inheritdoc />
    /// <remarks>
    /// The effective chunk size is the transport safe maximum, optionally lowered by a
    /// user-requested ceiling. A user request is treated as a ceiling only: it can make
    /// batches smaller (to bound lock duration) but is clamped down to the safe maximum so
    /// it can never overflow the database parameter limit. <see cref="BatchSize"/> further
    /// caps the result at the actual message count.
    /// </remarks>
    public class SendBatchSize : ISendBatchSize
    {
        private readonly int _effectiveMax;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendBatchSize"/> class.
        /// </summary>
        /// <param name="safeMaxBatchSize">The transport-computed safe maximum, derived from
        /// the database command parameter limit divided by the widest per-message parameter
        /// count. Must be at least 1.</param>
        /// <param name="requestedBatchSize">Optional user-requested ceiling. When supplied
        /// and at least 1, the effective maximum is the smaller of this value and
        /// <paramref name="safeMaxBatchSize"/>. Values below 1 are ignored.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="safeMaxBatchSize"/> is less than 1.</exception>
        public SendBatchSize(int safeMaxBatchSize, int? requestedBatchSize = null)
        {
            if (safeMaxBatchSize < 1)
                throw new ArgumentOutOfRangeException(nameof(safeMaxBatchSize), safeMaxBatchSize,
                    "The safe maximum batch size must be at least 1.");

            _effectiveMax = requestedBatchSize.HasValue && requestedBatchSize.Value >= 1
                ? Math.Min(requestedBatchSize.Value, safeMaxBatchSize)
                : safeMaxBatchSize;
        }

        /// <inheritdoc />
        public int BatchSize(int messageCount)
        {
            if (messageCount < 1)
                return 1;
            return Math.Min(_effectiveMax, messageCount);
        }
    }
}
