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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotNetWorkQueue.Dashboard.Api.Models;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;

namespace DotNetWorkQueue.Dashboard.Api.Services
{
    /// <summary>
    /// Implementation of <see cref="IDashboardService"/> that resolves query handlers from per-queue containers.
    /// </summary>
    internal class DashboardService : IDashboardService
    {
        private readonly IDashboardApi _dashboardApi;

        public DashboardService(IDashboardApi dashboardApi)
        {
            _dashboardApi = dashboardApi;
        }

        /// <inheritdoc />
        public IReadOnlyList<ConnectionResponse> GetConnections()
        {
            return _dashboardApi.Connections.Values.Select(c => new ConnectionResponse
            {
                Id = c.Id,
                DisplayName = c.DisplayName,
                QueueCount = c.Queues.Count
            }).ToList();
        }

        /// <inheritdoc />
        public IReadOnlyList<QueueInfoResponse> GetQueues(Guid connectionId)
        {
            if (!_dashboardApi.Connections.TryGetValue(connectionId, out var connection))
                throw new InvalidOperationException($"Connection id {connectionId} was not found");

            return connection.Queues.Select(q => new QueueInfoResponse
            {
                Id = q.Id,
                QueueName = q.QueueName
            }).ToList();
        }

        /// <inheritdoc />
        public QueueStatusResponse GetStatus(Guid queueId)
        {
            var container = GetContainer(queueId);
            var handler = container.GetInstance<IQueryHandler<GetDashboardStatusCountsQuery, DashboardStatusCounts>>();
            var result = handler.Handle(new GetDashboardStatusCountsQuery());
            return new QueueStatusResponse
            {
                Waiting = result.Waiting,
                Processing = result.Processing,
                Error = result.Error,
                Total = result.Total
            };
        }

        /// <inheritdoc />
        public QueueFeaturesResponse GetFeatures(Guid queueId)
        {
            var container = GetContainer(queueId);
            var factory = container.GetInstance<ITransportOptionsFactory>();
            var options = factory.Create();
            return new QueueFeaturesResponse
            {
                EnablePriority = options.EnablePriority,
                EnableStatus = options.EnableStatus,
                EnableStatusTable = options.EnableStatusTable,
                EnableHeartBeat = options.EnableHeartBeat,
                EnableDelayedProcessing = options.EnableDelayedProcessing,
                EnableMessageExpiration = options.EnableMessageExpiration,
                EnableRoute = options.EnableRoute
            };
        }

        /// <inheritdoc />
        public PagedResponse<MessageResponse> GetMessages(Guid queueId, int pageIndex, int pageSize, int? statusFilter)
        {
            var container = GetContainer(queueId);
            var handler = container.GetInstance<IQueryHandler<GetDashboardMessagesQuery, IReadOnlyList<DashboardMessage>>>();
            var result = handler.Handle(new GetDashboardMessagesQuery(pageIndex, pageSize, statusFilter));
            var totalCount = GetMessageCount(queueId, statusFilter);
            return new PagedResponse<MessageResponse>
            {
                Items = result.Select(MapMessage).ToList(),
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        /// <inheritdoc />
        public long GetMessageCount(Guid queueId, int? statusFilter)
        {
            var container = GetContainer(queueId);
            var handler = container.GetInstance<IQueryHandler<GetDashboardMessageCountQuery, long>>();
            return handler.Handle(new GetDashboardMessageCountQuery(statusFilter));
        }

        /// <inheritdoc />
        public MessageResponse GetMessageDetail(Guid queueId, long messageId)
        {
            var container = GetContainer(queueId);
            var handler = container.GetInstance<IQueryHandler<GetDashboardMessageDetailQuery, DashboardMessage>>();
            var result = handler.Handle(new GetDashboardMessageDetailQuery(messageId));
            return result != null ? MapMessage(result) : null;
        }

        /// <inheritdoc />
        public PagedResponse<MessageResponse> GetStaleMessages(Guid queueId, int thresholdSeconds, int pageIndex, int pageSize)
        {
            var container = GetContainer(queueId);
            var handler = container.GetInstance<IQueryHandler<GetDashboardStaleMessagesQuery, IReadOnlyList<DashboardMessage>>>();
            var result = handler.Handle(new GetDashboardStaleMessagesQuery(thresholdSeconds, pageIndex, pageSize));
            return new PagedResponse<MessageResponse>
            {
                Items = result.Select(MapMessage).ToList(),
                TotalCount = result.Count,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        /// <inheritdoc />
        public PagedResponse<ErrorMessageResponse> GetErrors(Guid queueId, int pageIndex, int pageSize)
        {
            var container = GetContainer(queueId);
            var handler = container.GetInstance<IQueryHandler<GetDashboardErrorMessagesQuery, IReadOnlyList<DashboardErrorMessage>>>();
            var result = handler.Handle(new GetDashboardErrorMessagesQuery(pageIndex, pageSize));

            var countHandler = container.GetInstance<IQueryHandler<GetDashboardErrorMessageCountQuery, long>>();
            var totalCount = countHandler.Handle(new GetDashboardErrorMessageCountQuery());

            return new PagedResponse<ErrorMessageResponse>
            {
                Items = result.Select(e => new ErrorMessageResponse
                {
                    Id = e.Id,
                    QueueId = e.QueueId,
                    LastException = e.LastException,
                    LastExceptionDate = e.LastExceptionDate
                }).ToList(),
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        /// <inheritdoc />
        public IReadOnlyList<ErrorRetryResponse> GetErrorRetries(Guid queueId, long messageId)
        {
            var container = GetContainer(queueId);
            var handler = container.GetInstance<IQueryHandler<GetDashboardErrorRetriesQuery, IReadOnlyList<DashboardErrorRetry>>>();
            var result = handler.Handle(new GetDashboardErrorRetriesQuery(messageId));
            return result.Select(r => new ErrorRetryResponse
            {
                ErrorTrackingId = r.ErrorTrackingId,
                QueueId = r.QueueId,
                ExceptionType = r.ExceptionType,
                RetryCount = r.RetryCount
            }).ToList();
        }

        /// <inheritdoc />
        public ConfigurationResponse GetConfiguration(Guid queueId)
        {
            var container = GetContainer(queueId);
            var handler = container.GetInstance<IQueryHandler<GetDashboardConfigurationQuery, byte[]>>();
            var result = handler.Handle(new GetDashboardConfigurationQuery());
            return new ConfigurationResponse
            {
                ConfigurationJson = result != null ? Encoding.UTF8.GetString(result) : null
            };
        }

        /// <inheritdoc />
        public IReadOnlyList<JobResponse> GetJobs(Guid queueId)
        {
            var container = GetContainer(queueId);

            var tableExistsHandler = container.GetInstance<IQueryHandler<GetTableExistsQuery, bool>>();
            var connectionInfo = container.GetInstance<IConnectionInformation>();
            var tableNameHelper = container.GetInstance<ITableNameHelper>();
            if (!tableExistsHandler.Handle(new GetTableExistsQuery(connectionInfo.ConnectionString, tableNameHelper.JobTableName)))
                return new List<JobResponse>();

            var handler = container.GetInstance<IQueryHandler<GetDashboardJobsQuery, IReadOnlyList<DashboardJob>>>();
            var result = handler.Handle(new GetDashboardJobsQuery());
            return result.Select(j => new JobResponse
            {
                JobName = j.JobName,
                JobEventTime = j.JobEventTime,
                JobScheduledTime = j.JobScheduledTime
            }).ToList();
        }

        /// <inheritdoc />
        public IReadOnlyList<JobResponse> GetJobsByConnection(Guid connectionId)
        {
            if (!_dashboardApi.Connections.TryGetValue(connectionId, out var connection))
                throw new InvalidOperationException($"Connection id {connectionId} was not found");

            if (connection.Queues.Count == 0)
                return new List<JobResponse>();

            return GetJobs(connection.Queues[0].Id);
        }

        private IContainer GetContainer(Guid queueId)
        {
            return _dashboardApi.GetQueueContainer(queueId);
        }

        private static MessageResponse MapMessage(DashboardMessage m)
        {
            return new MessageResponse
            {
                QueueId = m.QueueId,
                QueuedDateTime = m.QueuedDateTime,
                CorrelationId = m.CorrelationId,
                Status = m.Status,
                Priority = m.Priority,
                QueueProcessTime = m.QueueProcessTime,
                HeartBeat = m.HeartBeat,
                ExpirationTime = m.ExpirationTime,
                Route = m.Route
            };
        }
    }
}
