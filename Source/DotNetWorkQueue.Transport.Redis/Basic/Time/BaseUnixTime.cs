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
