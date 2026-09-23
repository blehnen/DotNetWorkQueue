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
using System.Diagnostics.CodeAnalysis;

namespace DotNetWorkQueue.Transport.Redis.Basic.Time
{
    /// <summary>
    /// The point every unix timestamp in this transport is measured from.
    /// </summary>
    /// <remarks>
    /// Scores written to the working set and the values read back out have to convert against the
    /// same instant or a message's time moves when it is read. Three copies of it used to be
    /// declared separately; this is the one they all now use. <c>DateTime.UnixEpoch</c> would say
    /// it more directly but arrived in netstandard2.1, and this library still builds for
    /// netstandard2.0 (GitHub #252).
    /// </remarks>
    internal static class Epoch
    {
        /// <summary>1970-01-01T00:00:00Z.</summary>
        [SuppressMessage("Minor Code Smell", "S6588:Use the \"UnixEpoch\" field instead of creating \"DateTime\" instances that point to the beginning of the Unix epoch",
            Justification = "DateTime.UnixEpoch does not exist on netstandard2.0, which this library targets (GitHub #252)")]
        internal static readonly DateTime Unix = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    }
}
