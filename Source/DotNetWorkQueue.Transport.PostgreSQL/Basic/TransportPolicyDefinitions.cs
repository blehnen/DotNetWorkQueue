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
using Polly;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
{
    /// <summary>
    ///
    /// </summary>
    public static class TransportPolicyDefinitions
    {
        /// <summary>
        /// A policy for retrying a failed command
        /// </summary>
        /// <value>
        /// A policy for retrying a failed command
        /// </value>
        /// <remarks>The expected type is <see cref="ResiliencePipeline"/>.</remarks>
        public static string RetryCommandHandler => "PostgreSQLRetryCommandHandler";

        /// <summary>
        /// A policy for retrying a failed command (async)
        /// </summary>
        /// <value>
        /// A policy for retrying a failed command
        /// </value>
        /// <remarks>V8 uses a unified pipeline. Returns the same key as <see cref="RetryCommandHandler"/>.</remarks>
        public static string RetryCommandHandlerAsync => "PostgreSQLRetryCommandHandler";

        /// <summary>
        /// A policy for retrying a failed query
        /// </summary>
        /// <value>
        /// The retry query handler.
        /// </value>
        public static string RetryQueryHandler => "PostgreSQLRetryQueryHandler";
    }
}
