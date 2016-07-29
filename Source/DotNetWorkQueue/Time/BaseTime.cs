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
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;

namespace DotNetWorkQueue.Time
{
    /// <summary>
    /// Base server time module
    /// </summary>
    public abstract class BaseTime: IGetTime
    {
        private readonly object _getTime = new object();

        /// <summary>
        /// The configuration
        /// </summary>
        protected BaseTimeConfiguration Configuration;

        /// <summary>
        /// The log
        /// </summary>
        protected readonly ILog Log;

        /// <summary>
        /// The current server offset compared to local time
        /// </summary>
        protected TimeSpan Offset;

        /// <summary>
        /// Last time the server offset was calculated
        /// </summary>
        protected DateTime ServerOffsetObtained;

        /// <summary>
        /// Gets the time as a UTC date
        /// </summary>
        /// <returns></returns>
        protected abstract DateTime GetTime();

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseTime" /> class.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="configuration">The configuration.</param>
        protected BaseTime(ILogFactory log,
            BaseTimeConfiguration configuration)
        {
            Guard.NotNull(() => log, log);
            Guard.NotNull(() => configuration, configuration);

            Configuration = configuration;
            Log = log.Create();
        }

        /// <summary>
        /// Gets the current UTC date.
        /// </summary>
        /// <returns></returns>
        public DateTime GetCurrentUtcDate()
        {
            if (!TimeExpired()) return DateTime.UtcNow.Add(Offset);
            lock (_getTime)
            {
                if (!TimeExpired())
                    return DateTime.UtcNow.Add(Offset);
                var time = GetTime();
                var localTime = DateTime.UtcNow;
                Offset = time - localTime;
                ServerOffsetObtained = localTime;
                Log.DebugFormat("[{0}] server difference is {1} MS", Name, Offset.TotalMilliseconds);
            }
            return DateTime.UtcNow.Add(Offset);
        }

        /// <summary>
        /// Gets the name of the time provider
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public abstract string Name { get; }

        /// <summary>
        /// Gets the get current calculated offset.
        /// </summary>
        /// <value>
        /// The get current offset.
        /// </value>
        public TimeSpan GetCurrentOffset => Offset;

        /// <summary>
        /// Returns true if the cached time offset has expired
        /// </summary>
        /// <returns></returns>
        protected bool TimeExpired()
        {
            return ServerOffsetObtained.Add(Configuration.RefreshTime) < DateTime.UtcNow;
        }
    }
}
