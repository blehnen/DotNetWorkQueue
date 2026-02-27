// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
using System.Data;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler
{
    internal class GetDashboardMessageDetailQueryHandler : IQueryHandler<GetDashboardMessageDetailQuery, DashboardMessage>
    {
        private readonly IPrepareQueryHandler<GetDashboardMessageDetailQuery, DashboardMessage> _prepareQuery;
        private readonly IDbConnectionFactory _dbConnectionFactory;
        private readonly IReadColumn _readColumn;
        private readonly Lazy<ITransportOptions> _options;

        public GetDashboardMessageDetailQueryHandler(
            IDbConnectionFactory dbConnectionFactory,
            IPrepareQueryHandler<GetDashboardMessageDetailQuery, DashboardMessage> prepareQuery,
            IReadColumn readColumn,
            ITransportOptionsFactory options)
        {
            Guard.NotNull(() => dbConnectionFactory, dbConnectionFactory);
            Guard.NotNull(() => prepareQuery, prepareQuery);
            Guard.NotNull(() => readColumn, readColumn);
            Guard.NotNull(() => options, options);

            _prepareQuery = prepareQuery;
            _dbConnectionFactory = dbConnectionFactory;
            _readColumn = readColumn;
            _options = new Lazy<ITransportOptions>(options.Create);
        }

        public DashboardMessage Handle(GetDashboardMessageDetailQuery query)
        {
            using (var connection = _dbConnectionFactory.Create())
            {
                connection.Open();
                using (var command = connection.CreateCommand())
                {
                    _prepareQuery.Handle(query, command, CommandStringTypes.GetDashboardMessageDetail);
                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return ReadMessage(reader);
                        }
                    }
                }
            }
            return null;
        }

        private DashboardMessage ReadMessage(IDataReader reader)
        {
            var message = new DashboardMessage();
            var columnIndex = 0;

            message.QueueId = _readColumn.ReadAsInt64(CommandStringTypes.GetDashboardMessageDetail, columnIndex++, reader);
            message.QueuedDateTime = _readColumn.ReadAsDateTimeOffset(CommandStringTypes.GetDashboardMessageDetail, columnIndex++, reader);
            message.CorrelationId = _readColumn.ReadAsString(CommandStringTypes.GetDashboardMessageDetail, columnIndex++, reader);

            var opts = _options.Value;
            if (opts.EnableStatus)
                message.Status = _readColumn.ReadAsInt32(CommandStringTypes.GetDashboardMessageDetail, columnIndex++, reader);
            if (opts.EnablePriority)
                message.Priority = _readColumn.ReadAsInt32(CommandStringTypes.GetDashboardMessageDetail, columnIndex++, reader);
            if (opts.EnableDelayedProcessing)
                message.QueueProcessTime = _readColumn.ReadAsDateTimeOffset(CommandStringTypes.GetDashboardMessageDetail, columnIndex++, reader);
            if (opts.EnableHeartBeat)
                message.HeartBeat = _readColumn.ReadAsDateTimeOffset(CommandStringTypes.GetDashboardMessageDetail, columnIndex++, reader);
            if (opts.EnableMessageExpiration)
                message.ExpirationTime = _readColumn.ReadAsDateTimeOffset(CommandStringTypes.GetDashboardMessageDetail, columnIndex++, reader);
            if (opts.EnableRoute)
                message.Route = _readColumn.ReadAsString(CommandStringTypes.GetDashboardMessageDetail, columnIndex++, reader);

            return message;
        }
    }
}
