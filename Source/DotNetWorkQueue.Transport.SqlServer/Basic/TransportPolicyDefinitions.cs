// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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

namespace DotNetWorkQueue.Transport.SqlServer.Basic
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
        /// <remarks><seealso cref="Policy"></seealso> is the expected type</remarks>
        public static string RetryCommandHandler => "SqlServerRetryCommandHandler";

        /// <summary>
        /// A policy for retrying a failed command
        /// </summary>
        /// <value>
        /// A policy for retrying a failed command
        /// </value>
        /// <remarks><seealso cref="Policy"></seealso> is the expected type</remarks>
        public static string RetryCommandHandlerAsync => "SqlServerRetryCommandHandlerAsync";

        /// <summary>
        /// A policy for retrying a failed query
        /// </summary>
        /// <value>
        /// The retry query handler.
        /// </value>
        public static string RetryQueryHandler => "SqlServerRetryQueryHandler";
    }
}
