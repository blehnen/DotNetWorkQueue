// ---------------------------------------------------------------------
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
using DotNetWorkQueue.Transport.Redis.Basic.Lua;
using DotNetWorkQueue.Transport.Redis.Basic.Query;

namespace DotNetWorkQueue.Transport.Redis.Basic.QueryHandler
{
    internal class GetHeaderQueryHandler : IQueryHandler<GetHeaderQuery, IDictionary<string, object>>
    {
        private readonly GetHeaderLua _lua;
        private readonly ICompositeSerialization _serialization;

        public GetHeaderQueryHandler(GetHeaderLua lua,
            ICompositeSerialization serialization)
        {
            _lua = lua;
            _serialization = serialization;
        }
        public IDictionary<string, object> Handle(GetHeaderQuery query)
        {
            var headers = _lua.Execute(query.Id.Id.Value.ToString());
            if (headers != null)
            {
                return _serialization.InternalSerializer.ConvertBytesTo<IDictionary<string, object>>(headers);
            }
            return null;
        }
    }
}
