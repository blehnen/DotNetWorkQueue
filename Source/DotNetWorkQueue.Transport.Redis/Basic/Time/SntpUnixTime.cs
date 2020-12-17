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
using System;
using DotNetWorkQueue.Logging;
using GuerrillaNtp;

namespace DotNetWorkQueue.Transport.Redis.Basic.Time
{
    /// <summary>
    /// Returns the current unix time, using an NTP server
    /// </summary>
    internal class SntpUnixTime : BaseUnixTime
    {
        private readonly object _getTime = new object();
        private long _millisecondsDifference;
        private readonly SntpTimeConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="SntpUnixTime" /> class.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="configuration">The configuration.</param>
        public SntpUnixTime(ILogger log, SntpTimeConfiguration configuration)
            : base(log, configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Gets the name of the time provider
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public override string Name => "SNTP";

        /// <summary>
        /// Gets the time from the server as needed, based on cache expiration
        /// </summary>
        /// <returns></returns>
        protected override long GetUnixTime()
        {
            if (!TimeExpired()) return (long) (DateTime.UtcNow - UnixEpoch).TotalMilliseconds + _millisecondsDifference;

            lock (_getTime)
            {
                if (!TimeExpired())
                    return (long) (DateTime.UtcNow - UnixEpoch).TotalMilliseconds + _millisecondsDifference;

                using (var ntp = new NtpClient(_configuration.Server, _configuration.Port))
                {
                    ntp.Timeout = _configuration.TimeOut;
                    var ntpResponse = ntp.Query();
                    _millisecondsDifference = (long)ntpResponse.CorrectionOffset.TotalMilliseconds;
                }
                ServerOffsetObtained = DateTime.UtcNow;
            }
            return (long)(DateTime.UtcNow - UnixEpoch).TotalMilliseconds + _millisecondsDifference;
        }
    }
}
