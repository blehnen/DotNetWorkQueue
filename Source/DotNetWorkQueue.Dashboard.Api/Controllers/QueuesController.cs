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
using DotNetWorkQueue.Dashboard.Api.Models;
using DotNetWorkQueue.Dashboard.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DotNetWorkQueue.Dashboard.Api.Controllers
{
    /// <summary>
    /// API controller for per-queue dashboard operations.
    /// </summary>
    [ApiController]
    [Route("api/v1/dashboard/queues")]
    [Produces("application/json")]
    public class QueuesController : ControllerBase
    {
        private readonly IDashboardService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueuesController"/> class.
        /// </summary>
        public QueuesController(IDashboardService service)
        {
            _service = service;
        }

        /// <summary>
        /// Gets queue status counts (waiting, processing, error, total).
        /// </summary>
        [HttpGet("{queueId:guid}/status")]
        [ProducesResponseType(typeof(QueueStatusResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetStatus(Guid queueId)
        {
            return Ok(await _service.GetStatusAsync(queueId).ConfigureAwait(false));
        }

        /// <summary>
        /// Gets enabled transport features for a queue.
        /// </summary>
        [HttpGet("{queueId:guid}/features")]
        [ProducesResponseType(typeof(QueueFeaturesResponse), 200)]
        [ProducesResponseType(404)]
        public IActionResult GetFeatures(Guid queueId)
        {
            return Ok(_service.GetFeatures(queueId));
        }

        /// <summary>
        /// Gets maintenance service status for a queue.
        /// </summary>
        [HttpGet("{queueId:guid}/maintenance")]
        [ProducesResponseType(typeof(MaintenanceStatusResponse), 200)]
        [ProducesResponseType(404)]
        public IActionResult GetMaintenanceStatus(Guid queueId)
        {
            return Ok(_service.GetMaintenanceStatus(queueId));
        }

        /// <summary>
        /// Gets a paged list of messages, optionally filtered by status.
        /// </summary>
        [HttpGet("{queueId:guid}/messages")]
        [ProducesResponseType(typeof(PagedResponse<MessageResponse>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetMessages(Guid queueId,
            [FromQuery] int pageIndex = 0,
            [FromQuery] int pageSize = 25,
            [FromQuery] int? status = null)
        {
            if (status.HasValue && !IsValidStatus(status.Value))
                return BadRequest($"Invalid status value '{status.Value}'. Valid values are: 0 (Waiting), 1 (Processing), 2 (Error), 3 (Processed).");

            return Ok(await _service.GetMessagesAsync(queueId, pageIndex, pageSize, status).ConfigureAwait(false));
        }

        /// <summary>
        /// Gets the message count, optionally filtered by status.
        /// </summary>
        [HttpGet("{queueId:guid}/messages/count")]
        [ProducesResponseType(typeof(long), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetMessageCount(Guid queueId, [FromQuery] int? status = null)
        {
            if (status.HasValue && !IsValidStatus(status.Value))
                return BadRequest($"Invalid status value '{status.Value}'. Valid values are: 0 (Waiting), 1 (Processing), 2 (Error), 3 (Processed).");

            return Ok(await _service.GetMessageCountAsync(queueId, status).ConfigureAwait(false));
        }

        /// <summary>
        /// Gets a single message detail by message ID.
        /// </summary>
        [HttpGet("{queueId:guid}/messages/{messageId}")]
        [ProducesResponseType(typeof(MessageResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetMessageDetail(Guid queueId, string messageId)
        {
            var result = await _service.GetMessageDetailAsync(queueId, messageId).ConfigureAwait(false);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        /// <summary>
        /// Gets messages with stale heartbeats (processing but not sending heartbeats).
        /// </summary>
        [HttpGet("{queueId:guid}/messages/stale")]
        [ProducesResponseType(typeof(PagedResponse<MessageResponse>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetStaleMessages(Guid queueId,
            [FromQuery] int thresholdSeconds = 60,
            [FromQuery] int pageIndex = 0,
            [FromQuery] int pageSize = 25)
        {
            return Ok(await _service.GetStaleMessagesAsync(queueId, thresholdSeconds, pageIndex, pageSize).ConfigureAwait(false));
        }

        /// <summary>
        /// Gets a paged list of error messages.
        /// </summary>
        [HttpGet("{queueId:guid}/errors")]
        [ProducesResponseType(typeof(PagedResponse<ErrorMessageResponse>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetErrors(Guid queueId,
            [FromQuery] int pageIndex = 0,
            [FromQuery] int pageSize = 25)
        {
            return Ok(await _service.GetErrorsAsync(queueId, pageIndex, pageSize).ConfigureAwait(false));
        }

        /// <summary>
        /// Gets error retry tracking records for a specific message.
        /// </summary>
        [HttpGet("{queueId:guid}/messages/{messageId}/retries")]
        [ProducesResponseType(typeof(IReadOnlyList<ErrorRetryResponse>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetErrorRetries(Guid queueId, string messageId)
        {
            return Ok(await _service.GetErrorRetriesAsync(queueId, messageId).ConfigureAwait(false));
        }

        /// <summary>
        /// Gets queue configuration as JSON.
        /// </summary>
        [HttpGet("{queueId:guid}/configuration")]
        [ProducesResponseType(typeof(ConfigurationResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetConfiguration(Guid queueId)
        {
            return Ok(await _service.GetConfigurationAsync(queueId).ConfigureAwait(false));
        }

        /// <summary>
        /// Gets the decoded message body for a specific message.
        /// </summary>
        [HttpGet("{queueId:guid}/messages/{messageId}/body")]
        [ProducesResponseType(typeof(MessageBodyResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetMessageBody(Guid queueId, string messageId)
        {
            var result = await _service.GetMessageBodyAsync(queueId, messageId).ConfigureAwait(false);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        /// <summary>
        /// Gets the message headers for a specific message.
        /// </summary>
        [HttpGet("{queueId:guid}/messages/{messageId}/headers")]
        [ProducesResponseType(typeof(MessageHeadersResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetMessageHeaders(Guid queueId, string messageId)
        {
            var result = await _service.GetMessageHeadersAsync(queueId, messageId).ConfigureAwait(false);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        /// <summary>
        /// Deletes a single message from the queue.
        /// </summary>
        [HttpDelete("{queueId:guid}/messages/{messageId}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteMessage(Guid queueId, string messageId)
        {
            var deleted = await _service.DeleteMessageAsync(queueId, messageId).ConfigureAwait(false);
            return deleted ? NoContent() : NotFound();
        }

        /// <summary>
        /// Deletes all error messages from the queue. Returns the number of records deleted.
        /// </summary>
        [HttpDelete("{queueId:guid}/errors")]
        [ProducesResponseType(typeof(DeleteAllResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteAllErrors(Guid queueId)
        {
            var count = await _service.DeleteAllErrorMessagesAsync(queueId).ConfigureAwait(false);
            return Ok(new DeleteAllResponse { Deleted = count });
        }

        /// <summary>
        /// Requeues an error message, moving it back to Waiting status.
        /// </summary>
        [HttpPost("{queueId:guid}/messages/{messageId}/requeue")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RequeueErrorMessage(Guid queueId, string messageId)
        {
            var result = await _service.RequeueErrorMessageAsync(queueId, messageId).ConfigureAwait(false);
            return result ? NoContent() : NotFound();
        }

        /// <summary>
        /// Resets a stale (Processing) message back to Waiting status.
        /// Returns 404 if the message was not found in Processing state.
        /// </summary>
        [HttpPost("{queueId:guid}/messages/{messageId}/reset")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ResetStaleMessage(Guid queueId, string messageId)
        {
            var result = await _service.ResetStaleMessageAsync(queueId, messageId).ConfigureAwait(false);
            return result ? NoContent() : NotFound();
        }

        /// <summary>
        /// Requeues all error messages back to Waiting status. Returns the number of messages requeued.
        /// </summary>
        [HttpPost("{queueId:guid}/errors/requeue-all")]
        [ProducesResponseType(typeof(BulkActionResponse), 200)]
        public async Task<IActionResult> RequeueAllErrors(Guid queueId)
        {
            var count = await _service.RequeueAllErrorMessagesAsync(queueId).ConfigureAwait(false);
            return Ok(new BulkActionResponse { Count = count });
        }

        /// <summary>
        /// Resets all stale (Processing) messages back to Waiting status. Returns the number of messages reset.
        /// </summary>
        [HttpPost("{queueId:guid}/messages/reset-all")]
        [ProducesResponseType(typeof(BulkActionResponse), 200)]
        public async Task<IActionResult> ResetAllStaleMessages(Guid queueId)
        {
            var count = await _service.ResetAllStaleMessagesAsync(queueId).ConfigureAwait(false);
            return Ok(new BulkActionResponse { Count = count });
        }

        /// <summary>
        /// Re-encodes and saves a new message body. The JSON must be deserializable to the original
        /// message type; the operation is rejected if the type cannot be resolved.
        /// </summary>
        [HttpPut("{queueId:guid}/messages/{messageId}/body")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> EditMessageBody(Guid queueId, string messageId, [FromBody] EditMessageBodyRequest request)
        {
            if (request?.Body == null)
                return BadRequest("Body is required.");

            var result = await _service.EditMessageBodyAsync(queueId, messageId, request.Body).ConfigureAwait(false);
            return result switch
            {
                EditMessageBodyResult.Success => NoContent(),
                EditMessageBodyResult.NotFound => NotFound(),
                EditMessageBodyResult.TypeUnresolvable => BadRequest("Message type cannot be resolved; body cannot be safely round-tripped."),
                EditMessageBodyResult.MessageBeingProcessed => Conflict("Message is currently being processed and cannot be edited."),
                EditMessageBodyResult.InvalidJson => BadRequest("The supplied JSON could not be deserialized to the message's type."),
                _ => StatusCode(500)
            };
        }

        // === History ===

        /// <summary>
        /// Gets a paged list of message history records, optionally filtered by status.
        /// </summary>
        [HttpGet("{queueId:guid}/history")]
        [ProducesResponseType(typeof(PagedResponse<HistoryResponse>), 200)]
        public IActionResult GetHistory(Guid queueId,
            [FromQuery] int pageIndex = 0,
            [FromQuery] int pageSize = 25,
            [FromQuery] int? status = null)
        {
            return Ok(_service.GetHistory(queueId, pageIndex, pageSize, status));
        }

        /// <summary>
        /// Gets the history record for a specific message.
        /// </summary>
        [HttpGet("{queueId:guid}/history/{messageId}")]
        [ProducesResponseType(typeof(HistoryResponse), 200)]
        [ProducesResponseType(404)]
        public IActionResult GetHistoryByMessageId(Guid queueId, string messageId)
        {
            var result = _service.GetHistoryByMessageId(queueId, messageId);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        /// <summary>
        /// Gets the total count of history records, optionally filtered by status.
        /// </summary>
        [HttpGet("{queueId:guid}/history/count")]
        [ProducesResponseType(typeof(long), 200)]
        public IActionResult GetHistoryCount(Guid queueId, [FromQuery] int? status = null)
        {
            return Ok(_service.GetHistoryCount(queueId, status));
        }

        /// <summary>
        /// Purges history records older than the specified number of days (default 30).
        /// </summary>
        [HttpDelete("{queueId:guid}/history")]
        [ProducesResponseType(typeof(DeleteAllResponse), 200)]
        public IActionResult PurgeHistory(Guid queueId, [FromQuery] int? olderThanDays = null)
        {
            var count = _service.PurgeHistory(queueId, olderThanDays);
            return Ok(new DeleteAllResponse { Deleted = count });
        }

        private static bool IsValidStatus(int status)
        {
            return Enum.IsDefined(typeof(QueueStatuses), (short)status);
        }
    }
}
