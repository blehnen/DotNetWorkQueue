using System;

namespace DotNetWorkQueue.Transport.Redis
{
    /// <summary>
    /// Various operations around the unix time format
    /// </summary>
    public interface IUnixTime: IGetTime
    {
        /// <summary>
        /// Gets the current time as MS in unix time
        /// </summary>
        /// <returns></returns>
        long GetCurrentUnixTimestampMilliseconds();
        /// <summary>
        /// Gets the current time as MS in unix time, plus the passed in timespan.
        /// </summary>
        /// <param name="difference">The difference.</param>
        /// <returns></returns>
        long GetAddDifferenceMilliseconds(TimeSpan difference);
        /// <summary>
        /// Gets the current time as MS in unix time, minus the passed in timespan.
        /// </summary>
        /// <param name="difference">The difference.</param>
        /// <returns></returns>
        long GetSubtractDifferenceMilliseconds(TimeSpan difference);
        /// <summary>
        /// Returns a UTC date, based on the passed in milliseconds as unix time
        /// </summary>
        /// <param name="millis">The milliseconds.</param>
        /// <returns></returns>
        DateTime DateTimeFromUnixTimestampMilliseconds(long millis);
    }
}
