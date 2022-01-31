// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Transport.Redis.Basic.Time
{
    /// <summary>
    /// Returns the current unix time, using the local system clock
    /// </summary>
    /// <remarks>This should not be used in a multiple machine setup, unless the clocks are kept in sync</remarks>
    internal class LocalMachineUnixTime : BaseUnixTime
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisServerUnixTime" /> class.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="configuration">The configuration.</param>
        public LocalMachineUnixTime(ILogger log,
            BaseTimeConfiguration configuration) : base(log, configuration)
        {
        }

        /// <summary>
        /// Gets the name of the time provider
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public override string Name => "Local";

        /// <summary>
        /// Gets the time as a long unix format; precision must be milliseconds
        /// </summary>
        /// <returns></returns>
        protected override long GetUnixTime()
        {
            return (long)(DateTime.UtcNow - UnixEpoch).TotalMilliseconds;
        }
    }
}
