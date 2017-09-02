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
using System.Collections.Generic;

namespace DotNetWorkQueue.Configuration
{
    /// <summary>
    /// Contains information about retrying an exception
    /// </summary>
    public class RetryInformation : IRetryInformation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RetryInformation"/> class.
        /// </summary>
        /// <param name="exceptionType">Type of the exception.</param>
        /// <param name="times">The retry times.</param>
        public RetryInformation(Type exceptionType, List<TimeSpan> times)
        {
            ExceptionType = exceptionType;
            Times = times;
        }
        /// <summary>
        /// Gets the type of the exception.
        /// </summary>
        /// <value>
        /// The type of the exception.
        /// </value>
        public Type ExceptionType { get; }
        /// <summary>
        /// Gets the maximum number of retry attempts for <see cref="ExceptionType"/>
        /// </summary>
        /// <value>
        /// The maximum retries.
        /// </value>
        public int MaxRetries => Times?.Count ?? 0;

        /// <summary>
        /// Gets the retry delay times.
        /// </summary>
        /// <value>
        /// The times.
        /// </value>
        public List<TimeSpan> Times { get; }
    }
}
