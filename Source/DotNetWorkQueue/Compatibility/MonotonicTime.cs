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
using System.Diagnostics;

namespace DotNetWorkQueue.Compatibility
{
    /// <summary>
    /// Elapsed time from <see cref="Stopwatch.GetTimestamp"/> readings.
    /// </summary>
    /// <remarks>
    /// Stands in for <c>Stopwatch.GetElapsedTime</c>, which needs .NET 7 and so is not available on
    /// every target this library builds for. Written once rather than forwarded per target, so the
    /// arithmetic the tests cover is the arithmetic every target gets (GitHub #252).
    /// </remarks>
    internal static class MonotonicTime
    {
        private static readonly double TicksPerTimestamp = (double)TimeSpan.TicksPerSecond / Stopwatch.Frequency;

        /// <summary>
        /// The time elapsed since a timestamp taken from <see cref="Stopwatch.GetTimestamp"/>.
        /// </summary>
        /// <param name="startingTimestamp">The earlier timestamp.</param>
        public static TimeSpan ElapsedSince(long startingTimestamp) =>
            Elapsed(startingTimestamp, Stopwatch.GetTimestamp());

        /// <summary>
        /// The time between two timestamps taken from <see cref="Stopwatch.GetTimestamp"/>.
        /// </summary>
        /// <param name="startingTimestamp">The earlier timestamp.</param>
        /// <param name="endingTimestamp">The later timestamp.</param>
        public static TimeSpan Elapsed(long startingTimestamp, long endingTimestamp) =>
            new TimeSpan((long)((endingTimestamp - startingTimestamp) * TicksPerTimestamp));
    }
}
