using System;
using System.Diagnostics;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Transport.Redis.Basic.Lua;
using DotNetWorkQueue.Validation;

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
        /// Gets the name of the time provider
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public override string Name => "Redis";

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
