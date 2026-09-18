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
using System.Text;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
{
    /// <summary>
    /// Builds PostgreSQL identifiers that survive the server's length limit.
    /// </summary>
    /// <remarks>
    /// PostgreSQL truncates every identifier at 63 bytes and does not warn. Index and constraint names
    /// are unique per schema, so two identifiers that truncate to the same text collide, and the second
    /// CREATE fails - measured, with 42P07.
    ///
    /// Every name here is built from the queue's name, and nine of the thirteen this transport
    /// generates are longer than 63 bytes once the queue name approaches the 51 character limit #339
    /// set. The worst leaves only 27 characters of it. That limit prevents two identifiers of the
    /// <em>same</em> queue from colliding, which is what it was measured against; it cannot prevent two
    /// <em>different</em> queues from doing so, because any two names may share a prefix.
    ///
    /// Nothing reads these names back - an index is found by its shape, and dropping a table takes its
    /// indexes with it - so a name only has to be valid, unique and stable (GitHub #375).
    /// </remarks>
    public static class PostgreSqlIdentifier
    {
        /// <summary>
        /// PostgreSQL's identifier limit. NAMEDATALEN - 1, and not configurable in a stock server.
        /// </summary>
        public const int MaxLengthInBytes = 63;

        /// <summary>
        /// The name itself when it fits, and a shortened, collision-resistant form when it does not.
        /// </summary>
        /// <param name="name">The identifier being built.</param>
        /// <remarks>
        /// A name that fits is returned untouched, so the ordinary case stays readable in the database
        /// and matches what earlier versions produced. Only an over-long name is shortened, and it
        /// keeps as much of the original as will fit before a hash of the whole of it, so that two
        /// names sharing a prefix do not share a result.
        ///
        /// FNV-1a rather than <see cref="object.GetHashCode"/>: string hash codes are randomised per
        /// process in .NET, so the same queue would be given a different index name on every run. The
        /// same reasoning as <c>SchemaUpgradeLockName</c>.
        /// </remarks>
        /// <returns>An identifier of at most <see cref="MaxLengthInBytes"/> bytes.</returns>
        public static string Shorten(string name)
        {
            if (string.IsNullOrEmpty(name) || Encoding.UTF8.GetByteCount(name) <= MaxLengthInBytes)
                return name;

            var suffix = "_" + Fnv1A(name).ToString("x16");
            var room = MaxLengthInBytes - suffix.Length;

            return string.Concat(TruncateToBytes(name, room), suffix);
        }

        /// <summary>
        /// The longest prefix of <paramref name="value"/> that fits in <paramref name="maxBytes"/>,
        /// without splitting a character across the boundary.
        /// </summary>
        private static string TruncateToBytes(string value, int maxBytes)
        {
            if (maxBytes <= 0)
                return string.Empty;

            //queue names are limited to alphanumerics and underscores, so this is one byte per
            //character in practice. It is done by byte anyway, because the limit is a byte limit and a
            //name that was not validated here would otherwise be cut in the wrong place.
            var length = Math.Min(value.Length, maxBytes);
            while (length > 0 && Encoding.UTF8.GetByteCount(value.Substring(0, length)) > maxBytes)
            {
                length--;
            }

            return value.Substring(0, length);
        }

        private static ulong Fnv1A(string value)
        {
            const ulong offsetBasis = 14695981039346656037;
            const ulong prime = 1099511628211;

            var hash = offsetBasis;
            foreach (var b in Encoding.UTF8.GetBytes(value))
            {
                hash ^= b;
                hash *= prime;
            }

            return hash;
        }
    }
}
