using System;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;

namespace DotNetWorkQueue.Transport.Redis.Basic.Time
{
    /// <summary>
    /// Returns the current unix time, using the local system clock
    /// </summary>
    /// <remarks>This should not be used in a multiple machine setup, unless the clocks are kept in sync</remarks>
    internal class LocalMachineUnixTime: BaseUnixTime
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisServerUnixTime" /> class.
        /// </summary>
        /// <param name="log">The log.</param>
        /// <param name="configuration">The configuration.</param>
        public LocalMachineUnixTime(ILogFactory log,
            BaseTimeConfiguration configuration): base(log, configuration)
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
