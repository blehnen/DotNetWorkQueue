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
using System.Collections.Generic;
using DotNetWorkQueue.Transport.Redis.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Query;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// Obtains header records
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IGetHeader" />
    public class GetHeader: IGetHeader
    {
        private readonly IQueryHandler<GetHeaderQuery<string>, IDictionary<string, object>> _queryHandler;
        /// <summary>
        /// Initializes a new instance of the <see cref="GetHeader"/> class.
        /// </summary>
        /// <param name="queryHandler">The query handler.</param>
        public GetHeader(IQueryHandler<GetHeaderQuery<string>, IDictionary<string, object>> queryHandler)
        {
            _queryHandler = queryHandler;
        }
        /// <inheritdoc />
        public IDictionary<string, object> GetHeaders(IMessageId id)
        {
            return _queryHandler.Handle(new GetHeaderQuery<string>(id.Id.Value.ToString()));
        }
    }
}
