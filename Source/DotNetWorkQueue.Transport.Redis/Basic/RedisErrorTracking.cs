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
using System.Collections.Generic;
namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// Tracks the exceptions that have occurred in user code while processing a message
    /// </summary>
    public class RedisErrorTracking
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisErrorTracking"/> class.
        /// </summary>
        public RedisErrorTracking()
        {
            Errors = new Dictionary<string, int>();
        }

        /// <summary>
        /// Gets the errors.
        /// </summary>
        /// <value>
        /// The errors.
        /// </value>
        /// <remarks>This is public, otherwise it may not be serialized, depending on the engine the user has chosen</remarks>
        public Dictionary<string, int> Errors{ get; set; }

        /// <summary>
        /// Gets the exception count.
        /// </summary>
        /// <param name="exceptionType">Type of the exception.</param>
        /// <returns></returns>
        public int GetExceptionCount(string exceptionType)
        {
            return !Errors.ContainsKey(exceptionType) ? 0 : Errors[exceptionType];
        }

        /// <summary>
        /// Increments the exception count.
        /// </summary>
        /// <param name="exceptionType">Type of the exception.</param>
        public void IncrementExceptionCount(string exceptionType)
        {
            if (!Errors.ContainsKey(exceptionType))
            {
                Errors.Add(exceptionType, 1);
            }
            else
            {
                Errors[exceptionType] = Errors[exceptionType] + 1;
            }
        }
    }
}
