// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
namespace DotNetWorkQueue
{
    /// <summary>
    /// Holds information about what exceptions should result in a message retry, how many times to retry and how to long to pause between retries.
    /// </summary>
    public interface IRetryDelay: IReadonly, ISetReadonly
    {
        /// <summary>
        /// Adds the specified exception type.
        /// </summary>
        /// <param name="exceptionType">Type of the exception.</param>
        /// <param name="timeSpans">The retry time spans.</param>
        void Add(Type exceptionType, List<TimeSpan> timeSpans);
        /// <summary>
        /// Returns information about how often to retry a particular exception.
        /// </summary>
        /// <typeparam name="T">The type of the exception</typeparam>
        /// <param name="exception">The exception.</param>
        /// <returns></returns>
        IRetryInformation GetRetryAmount<T>(T exception) 
            where T : Exception;
    }
}
