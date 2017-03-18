// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
using DotNetWorkQueue.Time;

namespace DotNetWorkQueue.Transport.Redis.Basic.Time
{
    /// <summary>
    /// Base time client
    /// </summary>
    /// <remarks>Inherit from this to create a new time client</remarks>
    public abstract class BaseUnixTime: BaseTime, IUnixTime
    {
        /// <summary>
        /// The unix epoch
        /// </summary>
        /// <remarks>Use this to turn a UTC date into a unix time stamp</remarks>
        protected static readonly DateTime UnixEpoch =
             new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisServerUnixTime" /> class.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="configuration">The configuration.</param>
        protected BaseUnixTime(ILogFactory log,
            BaseTimeConfiguration configuration): base(log, configuration)
        {

        }

        /// <summary>
        /// Gets the current time as MS in unix time
        /// </summary>
        /// <returns></returns>
        public long GetCurrentUnixTimestampMilliseconds()
        {
            return GetUnixTime();
        }

        /// <summary>
        /// Gets the current time as MS in unix time, plus the passed in timespan.
        /// </summary>
        /// <param name="difference">The difference.</param>
        /// <returns></returns>
        public long GetAddDifferenceMilliseconds(TimeSpan difference)
        {
            var current = GetUnixTime();
            return current + Convert.ToInt64(difference.TotalMilliseconds);
        }

        /// <summary>
        /// Gets the current time as MS in unix time, minus the passed in timespan.
        /// </summary>
        /// <param name="difference">The difference.</param>
        /// <returns></returns>
        public long GetSubtractDifferenceMilliseconds(TimeSpan difference)
        {
            var current = GetUnixTime();
            return current - Convert.ToInt64(difference.TotalMilliseconds);
        }

        /// <summary>
        /// Returns a UTC date, based on the passed in milliseconds as unix time
        /// </summary>
        /// <param name="millis">The milliseconds.</param>
        /// <returns></returns>
        public DateTime DateTimeFromUnixTimestampMilliseconds(long millis)
        {
            return UnixEpoch.AddMilliseconds(millis);
        }

        /// <summary>
        /// Gets the time as a long unix format; precision must be milliseconds
        /// </summary>
        /// <returns></returns>
        protected abstract long GetUnixTime();

        /// <summary>
        /// Gets the time.
        /// </summary>
        /// <returns></returns>
        protected override DateTime GetTime()
        {
            return UnixEpoch.AddMilliseconds(GetCurrentUnixTimestampMilliseconds());
        }
    }
}
