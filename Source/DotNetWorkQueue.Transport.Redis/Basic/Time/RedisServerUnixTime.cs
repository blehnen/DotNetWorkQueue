// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Transport.Redis.Basic.Lua;
namespace DotNetWorkQueue.Transport.Redis.Basic.Time
{
    /// <summary>
    /// Returns the current unix time, using the redis server(s)
    /// </summary>
    /// <remarks>If multiple redis servers are being used, their clocks must be in sync</remarks>
    internal class RedisServerUnixTime : BaseUnixTime
    {
        private readonly TimeLua _timeLua;
        private readonly object _getTime = new object();
        private long _millisecondsDifference;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisServerUnixTime" /> class.
        /// </summary>
        /// <param name="timeLua">The time lua.</param>
        /// <param name="log">The log.</param>
        /// <param name="configuration">The configuration.</param>
        public RedisServerUnixTime(TimeLua timeLua,
            ILogFactory log,
            BaseTimeConfiguration configuration): base(log, configuration)
        {
            Guard.NotNull(() => timeLua, timeLua);
            _timeLua = timeLua;
        }

        /// <summary>
        /// Gets the time from the redis server as needed.
        /// </summary>
        /// <returns></returns>
        protected override long GetUnixTime()
        {
            if (!TimeExpired()) return (long) (DateTime.UtcNow - UnixEpoch).TotalMilliseconds + _millisecondsDifference;
            lock (_getTime)
            {
                if (!TimeExpired())
                    return (long) (DateTime.UtcNow - UnixEpoch).TotalMilliseconds + _millisecondsDifference;

                var sw = new Stopwatch();
                sw.Start();
                var unixTime = _timeLua.Execute();
                sw.Stop();
                        
                unixTime = unixTime + sw.ElapsedMilliseconds;

                var localTime = (long)(DateTime.UtcNow - UnixEpoch).TotalMilliseconds;
                _millisecondsDifference = (localTime - unixTime) * -1;
                ServerOffsetObtained = DateTime.UtcNow;
            }
            return (long)(DateTime.UtcNow - UnixEpoch).TotalMilliseconds + _millisecondsDifference;
        }
    }
}
