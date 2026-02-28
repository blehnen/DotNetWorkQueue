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
using DotNetWorkQueue.Dashboard.Api.Models;
using DotNetWorkQueue.Factory;
using DotNetWorkQueue.Serialization;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DotNetWorkQueue.Dashboard.Api.Services
{
    /// <summary>
    /// Implementation of <see cref="IDashboardService"/> that resolves query handlers from per-queue containers.
    /// </summary>
    internal class DashboardService : IDashboardService
    {
        private readonly IDashboardApi _dashboardApi;
        private readonly ILogger<DashboardService> _logger;

        public DashboardService(IDashboardApi dashboardApi, ILogger<DashboardService> logger)
        {
            _dashboardApi = dashboardApi;
            _logger = logger;
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
        public async Task<QueueStatusResponse> GetStatusAsync(Guid queueId)
        {
            var container = GetContainer(queueId);
            var handler = container.GetInstance<IQueryHandlerAsync<GetDashboardStatusCountsQuery, DashboardStatusCounts>>();
            var result = await handler.HandleAsync(new GetDashboardStatusCountsQuery()).ConfigureAwait(false);
            return new QueueStatusResponse
            {
                Waiting = result.Waiting,
                Processing = result.Processing,
                Error = result.Error,
                Total = result.Total
            };
        }

        /// <inheritdoc />
        public async Task<PagedResponse<MessageResponse>> GetMessagesAsync(Guid queueId, int pageIndex, int pageSize, int? statusFilter)
        {
            var container = GetContainer(queueId);
            var handler = container.GetInstance<IQueryHandlerAsync<GetDashboardMessagesQuery, IReadOnlyList<DashboardMessage>>>();
            var result = await handler.HandleAsync(new GetDashboardMessagesQuery(pageIndex, pageSize, statusFilter)).ConfigureAwait(false);
            var totalCount = await GetMessageCountAsync(queueId, statusFilter).ConfigureAwait(false);
            return new PagedResponse<MessageResponse>
            {
                Items = result.Select(MapMessage).ToList(),
                TotalCount = totalCount,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        /// <inheritdoc />
        public async Task<long> GetMessageCountAsync(Guid queueId, int? statusFilter)
        {
            var container = GetContainer(queueId);
            var handler = container.GetInstance<IQueryHandlerAsync<GetDashboardMessageCountQuery, long>>();
            return await handler.HandleAsync(new GetDashboardMessageCountQuery(statusFilter)).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<MessageResponse> GetMessageDetailAsync(Guid queueId, long messageId)
        {
            var container = GetContainer(queueId);
            var handler = container.GetInstance<IQueryHandlerAsync<GetDashboardMessageDetailQuery, DashboardMessage>>();
            var result = await handler.HandleAsync(new GetDashboardMessageDetailQuery(messageId)).ConfigureAwait(false);
            return result != null ? MapMessage(result) : null;
        }

        /// <inheritdoc />
        public async Task<PagedResponse<MessageResponse>> GetStaleMessagesAsync(Guid queueId, int thresholdSeconds, int pageIndex, int pageSize)
        {
            var container = GetContainer(queueId);
            var handler = container.GetInstance<IQueryHandlerAsync<GetDashboardStaleMessagesQuery, IReadOnlyList<DashboardMessage>>>();
            var result = await handler.HandleAsync(new GetDashboardStaleMessagesQuery(thresholdSeconds, pageIndex, pageSize)).ConfigureAwait(false);
            return new PagedResponse<MessageResponse>
            {
                Items = result.Select(MapMessage).ToList(),
                TotalCount = result.Count,
                PageIndex = pageIndex,
                PageSize = pageSize
            };
        }

        /// <inheritdoc />
        public async Task<PagedResponse<ErrorMessageResponse>> GetErrorsAsync(Guid queueId, int pageIndex, int pageSize)
        {
            var container = GetContainer(queueId);
            var handler = container.GetInstance<IQueryHandlerAsync<GetDashboardErrorMessagesQuery, IReadOnlyList<DashboardErrorMessage>>>();
            var result = await handler.HandleAsync(new GetDashboardErrorMessagesQuery(pageIndex, pageSize)).ConfigureAwait(false);

            var countHandler = container.GetInstance<IQueryHandlerAsync<GetDashboardErrorMessageCountQuery, long>>();
            var totalCount = await countHandler.HandleAsync(new GetDashboardErrorMessageCountQuery()).ConfigureAwait(false);

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
        public async Task<IReadOnlyList<ErrorRetryResponse>> GetErrorRetriesAsync(Guid queueId, long messageId)
        {
            var container = GetContainer(queueId);
            var handler = container.GetInstance<IQueryHandlerAsync<GetDashboardErrorRetriesQuery, IReadOnlyList<DashboardErrorRetry>>>();
            var result = await handler.HandleAsync(new GetDashboardErrorRetriesQuery(messageId)).ConfigureAwait(false);
            return result.Select(r => new ErrorRetryResponse
            {
                ErrorTrackingId = r.ErrorTrackingId,
                QueueId = r.QueueId,
                ExceptionType = r.ExceptionType,
                RetryCount = r.RetryCount
            }).ToList();
        }

        /// <inheritdoc />
        public async Task<ConfigurationResponse> GetConfigurationAsync(Guid queueId)
        {
            var container = GetContainer(queueId);
            var handler = container.GetInstance<IQueryHandlerAsync<GetDashboardConfigurationQuery, byte[]>>();
            var result = await handler.HandleAsync(new GetDashboardConfigurationQuery()).ConfigureAwait(false);
            return new ConfigurationResponse
            {
                ConfigurationJson = result != null ? Encoding.UTF8.GetString(result) : null
            };
        }

        private async Task<IReadOnlyList<JobResponse>> GetJobsAsync(Guid queueId)
        {
            var container = GetContainer(queueId);

            var tableExistsHandler = container.GetInstance<IQueryHandler<GetTableExistsQuery, bool>>();
            var connectionInfo = container.GetInstance<IConnectionInformation>();
            var tableNameHelper = container.GetInstance<ITableNameHelper>();
            if (!tableExistsHandler.Handle(new GetTableExistsQuery(connectionInfo.ConnectionString, tableNameHelper.JobTableName)))
                return new List<JobResponse>();

            var handler = container.GetInstance<IQueryHandlerAsync<GetDashboardJobsQuery, IReadOnlyList<DashboardJob>>>();
            var result = await handler.HandleAsync(new GetDashboardJobsQuery()).ConfigureAwait(false);
            return result.Select(j => new JobResponse
            {
                JobName = j.JobName,
                JobEventTime = j.JobEventTime,
                JobScheduledTime = j.JobScheduledTime
            }).ToList();
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<JobResponse>> GetJobsByConnectionAsync(Guid connectionId)
        {
            if (!_dashboardApi.Connections.TryGetValue(connectionId, out var connection))
                throw new InvalidOperationException($"Connection id {connectionId} was not found");

            if (connection.Queues.Count == 0)
                return new List<JobResponse>();

            return await GetJobsAsync(connection.Queues[0].Id).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<MessageBodyResponse> GetMessageBodyAsync(Guid queueId, long messageId)
        {
            var container = GetContainer(queueId);
            var handler = container.GetInstance<IQueryHandlerAsync<GetDashboardMessageBodyQuery, DashboardMessageBody>>();
            var result = await handler.HandleAsync(new GetDashboardMessageBodyQuery(messageId)).ConfigureAwait(false);
            if (result == null)
                return null;

            try
            {
                var serialization = container.GetInstance<ICompositeSerialization>();
                var headers = serialization.InternalSerializer.ConvertBytesTo<IDictionary<string, object>>(result.Headers);

                var standardHeaders = container.GetInstance<IHeaders>();
                var graph = (MessageInterceptorsGraph)headers[standardHeaders.StandardHeaders.MessageInterceptorGraph.Name];
                var interceptorTypes = graph.Types.Select(t => t.Name).ToList();
                var wasIntercepted = interceptorTypes.Count > 0;

                var decodedBody = serialization.Serializer.BytesToMessage<MessageBody>(result.Body, graph, headers).Body;
                var messageFactory = container.GetInstance<IMessageFactory>();
                var newMessage = messageFactory.Create(decodedBody, headers);

                var typeName = ((object)newMessage.Body)?.GetType().FullName;
                string bodyJson = null;

                // Attempt typed re-deserialization when the producer stamped a body type header.
                // Stage 1: type already loaded in AppDomain (embedded dashboard).
                // Stage 2: load assembly from bin folder (standalone dashboard with user DLLs present).
                if (headers.TryGetValue("Queue-MessageBodyType", out var rawTypeName) && rawTypeName is string portableName)
                {
                    var resolvedType = ResolveMessageBodyType(portableName);
                    if (resolvedType != null)
                    {
                        try
                        {
                            // Compact intermediate serialization — no indentation needed for the round-trip step.
                            var typedBody = JsonConvert.DeserializeObject(
                                JsonConvert.SerializeObject(newMessage.Body), resolvedType);
                            bodyJson = JsonConvert.SerializeObject(typedBody, Formatting.Indented,
                                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None });
                            typeName = resolvedType.FullName;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex,
                                "Type header present for {TypeName} but re-deserialization failed for message {MessageId}",
                                portableName, messageId);
                        }
                    }
                }

                // Fall back to generic JObject serialization if type header absent, unresolvable, or re-deserialization failed.
                bodyJson ??= JsonConvert.SerializeObject(newMessage.Body, Formatting.Indented,
                    new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.None });

                return new MessageBodyResponse
                {
                    Body = bodyJson,
                    TypeName = typeName,
                    WasIntercepted = wasIntercepted,
                    InterceptorChain = interceptorTypes
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decode body for message {MessageId} in queue {QueueId}", messageId, queueId);
                return new MessageBodyResponse
                {
                    DecodingError = ex.Message,
                    WasIntercepted = false,
                    InterceptorChain = new List<string>()
                };
            }
        }

        /// <inheritdoc />
        public async Task<MessageHeadersResponse> GetMessageHeadersAsync(Guid queueId, long messageId)
        {
            var container = GetContainer(queueId);
            var handler = container.GetInstance<IQueryHandlerAsync<GetDashboardMessageHeadersQuery, DashboardMessageHeaders>>();
            var result = await handler.HandleAsync(new GetDashboardMessageHeadersQuery(messageId)).ConfigureAwait(false);
            if (result == null)
                return null;

            try
            {
                var serialization = container.GetInstance<ICompositeSerialization>();
                var headers = serialization.InternalSerializer.ConvertBytesTo<IDictionary<string, object>>(result.Headers);
                return new MessageHeadersResponse
                {
                    Headers = SanitizeHeaders(headers)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decode headers for message {MessageId} in queue {QueueId}", messageId, queueId);
                return new MessageHeadersResponse
                {
                    DecodingError = ex.Message
                };
            }
        }

        private static IDictionary<string, object> SanitizeHeaders(IDictionary<string, object> headers)
        {
            var result = new Dictionary<string, object>(headers.Count);
            foreach (var kvp in headers)
                result[kvp.Key] = SanitizeHeaderValue(kvp.Value);
            return result;
        }

        private static object SanitizeHeaderValue(object value)
        {
            if (value == null) return null;
            if (value is string || value is bool || value is int || value is long ||
                value is double || value is float || value is decimal || value is Guid)
                return value;
            if (value is MessageInterceptorsGraph graph)
                return graph.Types.Select(t => t.FullName ?? t.Name).ToList();
            var type = value.GetType();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueTypeWrapper<>))
                return type.GetProperty("Value")?.GetValue(value);
            return value.ToString();
        }

        /// <summary>
        /// Attempts to resolve a portable type name ("TypeFullName, AssemblySimpleName") to a
        /// <see cref="Type"/>. Stage 1 checks the AppDomain (embedded scenario). Stage 2 tries
        /// to load the assembly from <see cref="AppContext.BaseDirectory"/> (standalone scenario
        /// where the user has placed their POCO DLLs in the dashboard's bin folder).
        /// Returns null if the type cannot be resolved; never throws.
        /// </summary>
        private static Type ResolveMessageBodyType(string portableName)
        {
            // Stage 1: already loaded in AppDomain
            var type = Type.GetType(portableName);
            if (type != null)
                return type;

            // Stage 2: try loading from bin folder
            // portableName format: "TypeFullName, AssemblySimpleName"
            var commaIndex = portableName.IndexOf(',');
            if (commaIndex < 0)
                return null;

            var typeFullName = portableName[..commaIndex].Trim();
            var assemblySimpleName = portableName[(commaIndex + 1)..].Trim();
            var assemblyPath = Path.Combine(AppContext.BaseDirectory, assemblySimpleName + ".dll");

            if (!File.Exists(assemblyPath))
                return null;

            try
            {
                var assembly = Assembly.LoadFrom(assemblyPath);
                return assembly.GetType(typeFullName);
            }
            catch
            {
                return null;
            }
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
