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
using DotNetWorkQueue.Configuration;

namespace DotNetWorkQueue.Transport.Redis.Basic.Time
{
    /// <summary>
    /// Configuration class for the sNTP client
    /// </summary>
    public class SntpTimeConfiguration: BaseTimeConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SntpTimeConfiguration"/> class.
        /// </summary>
        public SntpTimeConfiguration()
        {
            Server = "pool.ntp.org";
            Port = 123;
            TimeOut = TimeSpan.FromSeconds(8);
        }
        /// <summary>
        /// NTP server to use
        /// </summary>
        /// <value>
        /// The server.
        /// </value>
        /// <remarks>Default is pool.ntp.org</remarks>
        public string Server { get; set; }
        /// <summary>
        /// Gets or sets the ntp port.
        /// </summary>
        /// <value>
        /// The port.
        /// </value>
        /// <remarks>Default is 123</remarks>
        public int Port { get; set; }
        /// <summary>
        /// Gets or sets the time out for querying the ntp server
        /// </summary>
        /// <value>
        /// The time out.
        /// </value>
        /// <remarks>The default is 8 seconds</remarks>
        public TimeSpan TimeOut { get; set; }
    }
}
