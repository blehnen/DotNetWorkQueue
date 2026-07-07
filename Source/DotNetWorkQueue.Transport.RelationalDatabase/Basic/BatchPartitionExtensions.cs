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
using System.Collections.Generic;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    /// <summary>
    /// Splits a list into fixed-size, order-preserving chunks for batched multi-row inserts.
    /// </summary>
    /// <remarks>
    /// Relational counterpart to the Redis transport's partition helper (which is
    /// Redis-namespace only). Order is preserved so generated ids can be re-associated with
    /// the caller's input positions.
    /// </remarks>
    public static class BatchPartitionExtensions
    {
        /// <summary>
        /// Partitions <paramref name="source"/> into consecutive chunks of at most
        /// <paramref name="size"/> items, preserving order.
        /// </summary>
        /// <typeparam name="T">The element type.</typeparam>
        /// <param name="source">The list to partition.</param>
        /// <param name="size">The maximum chunk size. Must be at least 1.</param>
        /// <returns>The chunks, in order. The final chunk may contain fewer than
        /// <paramref name="size"/> items. An empty source yields no chunks.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is less than 1.</exception>
        public static IEnumerable<IReadOnlyList<T>> Partition<T>(this IReadOnlyList<T> source, int size)
        {
            ArgumentNullException.ThrowIfNull(source);
            if (size < 1)
                throw new ArgumentOutOfRangeException(nameof(size), size, "The chunk size must be at least 1.");

            for (var start = 0; start < source.Count; start += size)
            {
                var count = Math.Min(size, source.Count - start);
                var chunk = new List<T>(count);
                for (var offset = 0; offset < count; offset++)
                    chunk.Add(source[start + offset]);
                yield return chunk;
            }
        }
    }
}
