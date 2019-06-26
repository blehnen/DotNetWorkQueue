﻿// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2018 Brian Lehnen
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
using DotNetWorkQueue.Transport.Shared;

namespace DotNetWorkQueue.Transport.Redis.Basic.Query
{
    /// <summary>
    /// Input data for obtaining a header record
    /// </summary>
    public class GetHeaderQuery : IQuery<IDictionary<string, object>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GetHeaderQuery"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public GetHeaderQuery(RedisQueueId id)
        {
            Id = id;
        }
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public RedisQueueId Id { get; }
    }
}
