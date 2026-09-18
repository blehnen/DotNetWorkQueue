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
using Polly;

namespace DotNetWorkQueue.Policies
{
    /// <summary>
    /// Looks a resilience pipeline up in a way that tolerates the registry having been disposed.
    /// </summary>
    /// <remarks>
    /// The registry is a container singleton, so the container disposes it, and shutdown does not wait
    /// indefinitely for work already in flight: StopWorkers waits out two configured timeouts and then
    /// force terminates. A handler can therefore still be running when the registry goes, and asking a
    /// disposed one for a pipeline throws.
    ///
    /// There is no non-throwing alternative. Checking first would only move the window, because the
    /// registry can be disposed between the check and the call. Catching is the mechanical fix, and it
    /// lands on the behaviour the caller already has for a pipeline that is not registered: run the
    /// work without one. Losing a retry on the last call of a queue that is shutting down is a better
    /// outcome than an exception from a queue that was told to stop (GitHub #121, #135).
    /// </remarks>
    internal static class PolicyLookup
    {
        /// <summary>
        /// The named pipeline, or null if there is none - or if the registry has been disposed.
        /// </summary>
        /// <param name="policies">The policies.</param>
        /// <param name="name">The pipeline's name.</param>
        public static ResiliencePipeline PipelineOrNull(IPolicies policies, string name)
        {
            try
            {
                return policies.Registry.TryGetPipeline(name, out var pipeline) ? pipeline : null;
            }
            catch (ObjectDisposedException)
            {
                //shutdown race - see the class remarks
                return null;
            }
        }
    }
}
