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

namespace DotNetWorkQueue.Cache
{
    /// <summary>
    /// Defines the cache behavior for a type
    /// </summary>
    /// <typeparam name="TType">The type of the class that implements caching.</typeparam>
    /// <remarks>Only implemented for very specific interfaces</remarks>
    public class CachePolicy<TType> : ICachePolicy<TType>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CachePolicy{TType}"/> class.
        /// </summary>
        public CachePolicy()
        {
            SlidingExpiration = TimeSpan.FromMinutes(5);
        }

        /// <summary>
        /// Gets the sliding expiration.
        /// </summary>
        /// <value>
        /// The sliding expiration.
        /// </value>
        public TimeSpan SlidingExpiration { get; set; }
    }
}
