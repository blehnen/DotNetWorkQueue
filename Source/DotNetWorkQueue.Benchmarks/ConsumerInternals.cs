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
using System.Collections.Concurrent;
using System.Reflection;
using DotNetWorkQueue.IoC;

namespace DotNetWorkQueue.Benchmarks
{
    /// <summary>
    /// Reaches the container a consumer built for itself, so a benchmark can drive the receive
    /// chain directly.
    /// </summary>
    /// <remarks>
    /// A consumer cannot be driven a message at a time through its public surface -
    /// <c>Start</c> puts worker threads on the queue and takes the timing out of the benchmark's
    /// hands. Resolving out of the consumer's own container is what keeps the measurement honest:
    /// the rungs get the configured instances with their decorators, rather than a hand-built
    /// approximation that could drift from the real receive path.
    /// </remarks>
    internal static class ConsumerInternals
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S3011:Reflection should not be used to increase accessibility of classes, methods, or fields",
            Justification = "Deliberate, and confined to a benchmark that is never shipped. It throws with an explanatory " +
                            "message if the layout changes, rather than silently measuring the wrong thing.")]
        public static IContainer ContainerOf(object queueContainer)
        {
            var field = typeof(BaseContainer).GetField("Containers", Flags)
                        ?? throw new InvalidOperationException(
                            "BaseContainer no longer has a 'Containers' field; update ConsumerInternals.");

            var bag = (ConcurrentBag<IDisposable>)field.GetValue(queueContainer);
            foreach (var item in bag)
            {
                if (item is IContainer container) return container;
            }

            throw new InvalidOperationException(
                "No IContainer found on the queue container; update ConsumerInternals.");
        }
    }
}
