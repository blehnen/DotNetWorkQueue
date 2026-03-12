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
using DotNetWorkQueue.Dashboard.Api.Models;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Factory;
using DotNetWorkQueue.Serialization;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Transport.Shared.Basic;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
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
            var creation = container.GetInstance<IQueueCreation>();
            var options = creation.BaseTransportOptions;
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
        public async Task<MessageResponse> GetMessageDetailAsync(Guid queueId, string messageId)
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
        public async Task<IReadOnlyList<ErrorRetryResponse>> GetErrorRetriesAsync(Guid queueId, string messageId)
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

            // Only relational transports register ITableNameHelper; check the transport flag
            // instead of catching resolution failures.
            var queueInfo = _dashboardApi.FindQueue(queueId);
            if (queueInfo is { IsRelationalTransport: true })
            {
                var tableNameHelper = container.GetInstance<ITableNameHelper>();
                var tableExistsHandler = container.GetInstance<IQueryHandler<GetTableExistsQuery, bool>>();
                var connectionInfo = container.GetInstance<IConnectionInformation>();
                if (!tableExistsHandler.Handle(new GetTableExistsQuery(connectionInfo.ConnectionString, tableNameHelper.JobTableName)))
                    return new List<JobResponse>();
            }

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
        public async Task<MessageBodyResponse> GetMessageBodyAsync(Guid queueId, string messageId)
        {
            var container = GetContainer(queueId);
            var handler = container.GetInstance<IQueryHandlerAsync<GetDashboardMessageBodyQuery, DashboardMessageBody>>();
            var result = await handler.HandleAsync(new GetDashboardMessageBodyQuery(messageId)).ConfigureAwait(false);
            if (result == null)
                return null;

            // Parse headers first — this succeeds even when interceptors are misconfigured,
            // and lets us report which interceptors the message requires.
            ICompositeSerialization serialization = null;
            IDictionary<string, object> headers = null;
            MessageInterceptorsGraph graph;
            List<string> interceptorTypes;
            bool wasIntercepted;

            try
            {
                serialization = container.GetInstance<ICompositeSerialization>();
                headers = serialization.InternalSerializer.ConvertBytesTo<IDictionary<string, object>>(result.Headers);

                var standardHeaders = container.GetInstance<IHeaders>();
                graph = (MessageInterceptorsGraph)headers[standardHeaders.StandardHeaders.MessageInterceptorGraph.Name];
                interceptorTypes = graph.Types.Select(t => t.Name).ToList();
                wasIntercepted = interceptorTypes.Count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decode headers for message {MessageId} in queue {QueueId}", messageId, queueId);

                // Try to extract interceptor info from partially-parsed headers
                var partialInterceptors = new List<string>();
                try
                {
                    if (headers != null)
                    {
                        var standardHeaders = container.GetInstance<IHeaders>();
                        var partialGraph = (MessageInterceptorsGraph)headers[standardHeaders.StandardHeaders.MessageInterceptorGraph.Name];
                        partialInterceptors = partialGraph.Types.Select(t => t.Name).ToList();
                    }
                }
                catch
                {
                    // Best effort — if we can't get interceptor info, return empty list
                }

                var hasInterceptors = partialInterceptors.Count > 0;
                var errorMessage = hasInterceptors
                    ? $"Failed to decode message headers. The message uses interceptors [{string.Join(", ", partialInterceptors)}]. Details: {ex.Message}"
                    : $"Failed to decode message headers. Details: {ex.Message}";

                return new MessageBodyResponse
                {
                    DecodingError = errorMessage,
                    WasIntercepted = hasInterceptors,
                    InterceptorChain = partialInterceptors
                };
            }

            try
            {
                var decodedBody = serialization.Serializer.BytesToMessage<MessageBody>(result.Body, graph, headers).Body;
                var messageFactory = container.GetInstance<IMessageFactory>();
                var newMessage = messageFactory.Create(decodedBody, headers);

                var typeName = ((object)newMessage.Body)?.GetType().FullName;
                string bodyJson = null;

                // Attempt typed re-deserialization when the producer stamped a body type header.
                // Stage 1: type already loaded in AppDomain (embedded dashboard).
                // Stage 2: load assembly from bin folder (standalone dashboard with user DLLs present).
                if (headers.TryGetValue("Queue-MessageBodyType", out var rawTypeName) && rawTypeName?.ToString() is string portableName && portableName.Length > 0)
                {
                    // Always prefer the header type name for display — it's the actual message type
                    // even when the assembly isn't loaded and we can't resolve the CLR type.
                    typeName = portableName;

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
            catch (InterceptorException ex)
            {
                _logger.LogError(ex, "Interceptor failed to decode body for message {MessageId} in queue {QueueId}", messageId, queueId);

                var innerMessage = ex.InnerException?.Message;
                var errorMessage = $"Failed to decode message body — the message was encoded with interceptors [{string.Join(", ", interceptorTypes)}] " +
                    $"but decoding failed. Verify that the dashboard has the same interceptors configured with the correct keys/settings. " +
                    $"Details: {innerMessage ?? ex.Message}";

                return new MessageBodyResponse
                {
                    DecodingError = errorMessage,
                    WasIntercepted = wasIntercepted,
                    InterceptorChain = interceptorTypes
                };
            }
            catch (SerializationException ex)
            {
                _logger.LogError(ex, "Deserialization failed for message {MessageId} in queue {QueueId}", messageId, queueId);

                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                var errorMessage = $"Failed to deserialize message body. " +
                    $"The message type may not be loadable — ensure the assembly containing the message type is available in the dashboard's bin folder. " +
                    $"Details: {innerMessage}";

                return new MessageBodyResponse
                {
                    DecodingError = errorMessage,
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
                    WasIntercepted = wasIntercepted,
                    InterceptorChain = interceptorTypes
                };
            }
        }

        /// <inheritdoc />
        public async Task<MessageHeadersResponse> GetMessageHeadersAsync(Guid queueId, string messageId)
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
            // Handle transport-specific correlation ID types (e.g. RedisQueueCorrelationIdSerialized)
            // that have a Guid Id property but no ToString override.
            var idProp = type.GetProperty("Id");
            if (idProp != null && idProp.PropertyType == typeof(Guid))
                return idProp.GetValue(value)?.ToString();
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

        /// <inheritdoc />
        public Task<bool> DeleteMessageAsync(Guid queueId, string messageId)
        {
            var container = GetContainer(queueId);
            var handler = container.GetInstance<ICommandHandlerWithOutput<DashboardDeleteMessageCommand, long>>();
            var result = handler.Handle(new DashboardDeleteMessageCommand(messageId));
            return Task.FromResult(result > 0);
        }

        /// <inheritdoc />
        public Task<long> DeleteAllErrorMessagesAsync(Guid queueId)
        {
            var container = GetContainer(queueId);
            var handler = container.GetInstance<ICommandHandlerWithOutput<DashboardDeleteAllErrorMessagesCommand, long>>();
            var result = handler.Handle(new DashboardDeleteAllErrorMessagesCommand());
            return Task.FromResult(result);
        }

        /// <inheritdoc />
        public Task<bool> RequeueErrorMessageAsync(Guid queueId, string messageId)
        {
            var container = GetContainer(queueId);
            var handler = container.GetInstance<ICommandHandlerWithOutput<DashboardRequeueErrorMessageCommand, long>>();
            var result = handler.Handle(new DashboardRequeueErrorMessageCommand(messageId));
            return Task.FromResult(result > 0);
        }

        /// <inheritdoc />
        public Task<bool> ResetStaleMessageAsync(Guid queueId, string messageId)
        {
            var container = GetContainer(queueId);
            var handler = container.GetInstance<ICommandHandlerWithOutput<DashboardResetStaleMessageCommand, long>>();
            var result = handler.Handle(new DashboardResetStaleMessageCommand(messageId));
            return Task.FromResult(result > 0);
        }

        /// <inheritdoc />
        public Task<long> RequeueAllErrorMessagesAsync(Guid queueId)
        {
            var container = GetContainer(queueId);
            var handler = container.GetInstance<ICommandHandlerWithOutput<DashboardRequeueAllErrorMessagesCommand, long>>();
            var result = handler.Handle(new DashboardRequeueAllErrorMessagesCommand());
            return Task.FromResult(result);
        }

        /// <inheritdoc />
        public Task<long> ResetAllStaleMessagesAsync(Guid queueId)
        {
            var container = GetContainer(queueId);
            var handler = container.GetInstance<ICommandHandlerWithOutput<DashboardResetAllStaleMessagesCommand, long>>();
            var result = handler.Handle(new DashboardResetAllStaleMessagesCommand());
            return Task.FromResult(result);
        }

        /// <inheritdoc />
        public async Task<EditMessageBodyResult> EditMessageBodyAsync(Guid queueId, string messageId, string bodyJson)
        {
            var container = GetContainer(queueId);

            // Step 1: Load the raw body + headers from the DB.
            var bodyHandler = container.GetInstance<IQueryHandlerAsync<GetDashboardMessageBodyQuery, DashboardMessageBody>>();
            var rawData = await bodyHandler.HandleAsync(new GetDashboardMessageBodyQuery(messageId)).ConfigureAwait(false);
            if (rawData == null)
                return EditMessageBodyResult.NotFound;

            // Step 2: Decode headers and resolve the original message type.
            var serialization = container.GetInstance<ICompositeSerialization>();
            var standardHeaders = container.GetInstance<IHeaders>();

            IDictionary<string, object> headers;
            try
            {
                headers = serialization.InternalSerializer.ConvertBytesTo<IDictionary<string, object>>(rawData.Headers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to decode headers for message {MessageId} in queue {QueueId}", messageId, queueId);
                return EditMessageBodyResult.TypeUnresolvable;
            }

            // Resolve the message body type — prefer the explicit header, fall back to runtime type from deserialization.
            Type resolvedType = null;
            var portableName = headers.TryGetValue("Queue-MessageBodyType", out var rawTypeName) ? rawTypeName?.ToString() : null;
            if (!string.IsNullOrEmpty(portableName))
                resolvedType = ResolveMessageBodyType(portableName);

            // Fallback: decode the existing body and use its runtime type (handles messages produced before the header was added).
            if (resolvedType == null)
            {
                try
                {
                    var graph = (MessageInterceptorsGraph)headers[standardHeaders.StandardHeaders.MessageInterceptorGraph.Name];
                    var decodedBody = serialization.Serializer.BytesToMessage<MessageBody>(rawData.Body, graph, headers).Body;
                    resolvedType = ((object)decodedBody)?.GetType();
                }
                catch (InterceptorException ex)
                {
                    var interceptorNames = string.Join(", ",
                        ((MessageInterceptorsGraph)headers[standardHeaders.StandardHeaders.MessageInterceptorGraph.Name])
                        .Types.Select(t => t.Name));
                    _logger.LogWarning(ex,
                        "Interceptor failed to decode body for message {MessageId} — message uses interceptors [{Interceptors}]. " +
                        "Verify that the dashboard has the same interceptors configured with the correct keys/settings",
                        messageId, interceptorNames);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to decode existing body to infer type for message {MessageId}", messageId);
                }
            }

            if (resolvedType == null)
                return EditMessageBodyResult.TypeUnresolvable;

            // Step 3: Reject edits to messages currently being processed (status = 1).
            var detailHandler = container.GetInstance<IQueryHandlerAsync<GetDashboardMessageDetailQuery, DashboardMessage>>();
            var detail = await detailHandler.HandleAsync(new GetDashboardMessageDetailQuery(messageId)).ConfigureAwait(false);
            if (detail?.Status == 1)
                return EditMessageBodyResult.MessageBeingProcessed;

            // Step 4: Deserialize the caller-supplied JSON to the resolved type.
            object typedObj;
            try
            {
                typedObj = JsonConvert.DeserializeObject(bodyJson, resolvedType);
            }
            catch (Exception)
            {
                return EditMessageBodyResult.InvalidJson;
            }
            if (typedObj == null)
                return EditMessageBodyResult.InvalidJson;

            // Step 5: Re-encode the new body through the same serializer+interceptor pipeline
            //         used when the message was originally produced.
            var encResult = serialization.Serializer.MessageToBytes(new MessageBody { Body = typedObj }, headers);

            // Step 6: Store the updated interceptor graph back in headers so the consumer can
            //         reverse it in the correct order.
            headers[standardHeaders.StandardHeaders.MessageInterceptorGraph.Name] = encResult.Graph;

            // Step 7: Serialize the updated headers to bytes.
            var newHeaderBytes = serialization.InternalSerializer.ConvertToBytes<IDictionary<string, object>>(headers);

            // Step 8: Persist body + headers.
            var updateHandler = container.GetInstance<ICommandHandlerWithOutput<DashboardUpdateMessageBodyCommand, long>>();
            updateHandler.Handle(new DashboardUpdateMessageBodyCommand(messageId, encResult.Output, newHeaderBytes));

            return EditMessageBodyResult.Success;
        }

        /// <inheritdoc />
        public MaintenanceStatusResponse GetMaintenanceStatus(Guid queueId)
        {
            var queueInfo = _dashboardApi.FindQueue(queueId);
            if (queueInfo == null)
                throw new InvalidOperationException($"Queue id {queueId} was not found");

            var service = _dashboardApi.GetMaintenanceService(queueId);
            return new MaintenanceStatusResponse
            {
                HostMaintenance = queueInfo.HostMaintenance,
                IsRunning = service?.IsRunning ?? false,
                LastRunUtc = service?.LastRun
            };
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
