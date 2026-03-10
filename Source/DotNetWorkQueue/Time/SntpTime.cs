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
using DotNetWorkQueue.Configuration;
using GuerrillaNtp;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Time
{
    /// <summary>
    /// Returns the current UTC time by querying an NTP server and caching the offset.
    /// </summary>
    public class SntpTime : BaseTime
    {
        private readonly SntpTimeConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="SntpTime"/> class.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="configuration">The SNTP configuration.</param>
        public SntpTime(ILogger log, SntpTimeConfiguration configuration)
            : base(log, configuration)
        {
            _configuration = configuration;
        }

        /// <inheritdoc />
        public override string Name => "SNTP";

        /// <inheritdoc />
        protected override DateTime GetTime()
        {
            var client = new NtpClient(_configuration.Server);
            var clock = client.Query();
            return DateTime.UtcNow.Add(clock.CorrectionOffset);
        }
    }
}
