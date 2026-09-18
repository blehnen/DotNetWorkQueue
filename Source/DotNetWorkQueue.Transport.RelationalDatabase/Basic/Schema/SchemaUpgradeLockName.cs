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
using System.Text;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.Schema
{
    /// <summary>
    /// Names the schema upgrade lock for a queue, the same way in every process.
    /// </summary>
    public static class SchemaUpgradeLockName
    {
        private const string Prefix = "DotNetWorkQueue.SchemaUpgrade.";

        /// <summary>
        /// The lock name for a queue, for transports whose locks are named.
        /// </summary>
        /// <param name="queueName">The queue being upgraded.</param>
        /// <remarks>
        /// Prefixed so it cannot collide with an application lock the host already uses for something
        /// of its own that happens to share a queue's name.
        /// </remarks>
        public static string For(string queueName) => string.Concat(Prefix, queueName);

        /// <summary>
        /// The lock key for a queue, for transports whose locks are numeric.
        /// </summary>
        /// <param name="queueName">The queue being upgraded.</param>
        /// <remarks>
        /// FNV-1a rather than <see cref="object.GetHashCode"/>, which is the point of this method
        /// existing: string hash codes are randomised per process in .NET, so two processes would
        /// derive different keys for the same queue and neither would block the other. A lock that
        /// silently fails to exclude is worse than no lock, because it looks like one.
        ///
        /// A 64-bit hash can collide, and a collision here costs one queue's upgrade waiting behind an
        /// unrelated one - a delay, not a correctness problem.
        /// </remarks>
        public static long KeyFor(string queueName)
        {
            const ulong offsetBasis = 14695981039346656037;
            const ulong prime = 1099511628211;

            var bytes = Encoding.UTF8.GetBytes(For(queueName));
            var hash = offsetBasis;
            foreach (var b in bytes)
            {
                hash ^= b;
                hash *= prime;
            }

            return unchecked((long)hash);
        }
    }
}
