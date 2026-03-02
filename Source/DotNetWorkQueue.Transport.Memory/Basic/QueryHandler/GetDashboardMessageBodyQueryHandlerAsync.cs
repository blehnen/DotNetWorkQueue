// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using System.Threading.Tasks;
using DotNetWorkQueue.Serialization;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Memory.Basic.QueryHandler
{
    internal class GetDashboardMessageBodyQueryHandlerAsync : IQueryHandlerAsync<GetDashboardMessageBodyQuery, DashboardMessageBody>
    {
        private readonly IDataStorage _dataStorage;
        private readonly ICompositeSerialization _serialization;
        private readonly IHeaders _headers;

        public GetDashboardMessageBodyQueryHandlerAsync(
            IDataStorage dataStorage,
            ICompositeSerialization serialization,
            IHeaders headers)
        {
            Guard.NotNull(() => dataStorage, dataStorage);
            Guard.NotNull(() => serialization, serialization);
            Guard.NotNull(() => headers, headers);

            _dataStorage = dataStorage;
            _serialization = serialization;
            _headers = headers;
        }

        public Task<DashboardMessageBody> HandleAsync(GetDashboardMessageBodyQuery query)
        {
            var item = _dataStorage.FindMessage(Guid.Parse(query.MessageId), out _);
            if (item == null)
                return Task.FromResult<DashboardMessageBody>(null);

            // Memory stores live objects — serialize on-the-fly for the dashboard pipeline
            var headerDict = item.Headers ?? new Dictionary<string, object>();
            var bodyResult = _serialization.Serializer.MessageToBytes(new MessageBody { Body = item.Body }, headerDict);

            // Update interceptor graph in headers so the dashboard service can decode properly
            headerDict[_headers.StandardHeaders.MessageInterceptorGraph.Name] = bodyResult.Graph;

            var internalSerializer = _serialization.InternalSerializer;
            var headersBytes = internalSerializer.ConvertToBytes(headerDict);

            return Task.FromResult(new DashboardMessageBody
            {
                Body = bodyResult.Output,
                Headers = headersBytes
            });
        }
    }
}
