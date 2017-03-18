// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
