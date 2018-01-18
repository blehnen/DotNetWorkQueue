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
