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
            return Ok(await _service.GetStatusAsync(queueId));
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

            return Ok(await _service.GetMessagesAsync(queueId, pageIndex, pageSize, status));
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

            return Ok(await _service.GetMessageCountAsync(queueId, status));
        }

        /// <summary>
        /// Gets a single message detail by message ID.
        /// </summary>
        [HttpGet("{queueId:guid}/messages/{messageId:long}")]
        [ProducesResponseType(typeof(MessageResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetMessageDetail(Guid queueId, long messageId)
        {
            var result = await _service.GetMessageDetailAsync(queueId, messageId);
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
            return Ok(await _service.GetStaleMessagesAsync(queueId, thresholdSeconds, pageIndex, pageSize));
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
            return Ok(await _service.GetErrorsAsync(queueId, pageIndex, pageSize));
        }

        /// <summary>
        /// Gets error retry tracking records for a specific message.
        /// </summary>
        [HttpGet("{queueId:guid}/messages/{messageId:long}/retries")]
        [ProducesResponseType(typeof(IReadOnlyList<ErrorRetryResponse>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetErrorRetries(Guid queueId, long messageId)
        {
            return Ok(await _service.GetErrorRetriesAsync(queueId, messageId));
        }

        /// <summary>
        /// Gets queue configuration as JSON.
        /// </summary>
        [HttpGet("{queueId:guid}/configuration")]
        [ProducesResponseType(typeof(ConfigurationResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetConfiguration(Guid queueId)
        {
            return Ok(await _service.GetConfigurationAsync(queueId));
        }

        /// <summary>
        /// Gets the decoded message body for a specific message.
        /// </summary>
        [HttpGet("{queueId:guid}/messages/{messageId:long}/body")]
        [ProducesResponseType(typeof(MessageBodyResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetMessageBody(Guid queueId, long messageId)
        {
            var result = await _service.GetMessageBodyAsync(queueId, messageId);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        /// <summary>
        /// Gets the message headers for a specific message.
        /// </summary>
        [HttpGet("{queueId:guid}/messages/{messageId:long}/headers")]
        [ProducesResponseType(typeof(MessageHeadersResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetMessageHeaders(Guid queueId, long messageId)
        {
            var result = await _service.GetMessageHeadersAsync(queueId, messageId);
            if (result == null)
                return NotFound();
            return Ok(result);
        }

        /// <summary>
        /// Deletes a single message from the queue.
        /// </summary>
        [HttpDelete("{queueId:guid}/messages/{messageId:long}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteMessage(Guid queueId, long messageId)
        {
            var deleted = await _service.DeleteMessageAsync(queueId, messageId);
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
            var count = await _service.DeleteAllErrorMessagesAsync(queueId);
            return Ok(new DeleteAllResponse { Deleted = count });
        }

        /// <summary>
        /// Requeues an error message, moving it back to Waiting status.
        /// </summary>
        [HttpPost("{queueId:guid}/messages/{messageId:long}/requeue")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> RequeueErrorMessage(Guid queueId, long messageId)
        {
            var result = await _service.RequeueErrorMessageAsync(queueId, messageId);
            return result ? NoContent() : NotFound();
        }

        /// <summary>
        /// Resets a stale (Processing) message back to Waiting status.
        /// Returns 404 if the message was not found in Processing state.
        /// </summary>
        [HttpPost("{queueId:guid}/messages/{messageId:long}/reset")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ResetStaleMessage(Guid queueId, long messageId)
        {
            var result = await _service.ResetStaleMessageAsync(queueId, messageId);
            return result ? NoContent() : NotFound();
        }

        /// <summary>
        /// Re-encodes and saves a new message body. The JSON must be deserializable to the original
        /// message type; the operation is rejected if the type cannot be resolved.
        /// </summary>
        [HttpPut("{queueId:guid}/messages/{messageId:long}/body")]
        [ProducesResponseType(204)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> EditMessageBody(Guid queueId, long messageId, [FromBody] EditMessageBodyRequest request)
        {
            if (request?.Body == null)
                return BadRequest("Body is required.");

            var result = await _service.EditMessageBodyAsync(queueId, messageId, request.Body);
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

        private static bool IsValidStatus(int status)
        {
            return Enum.IsDefined(typeof(QueueStatuses), (short)status);
        }
    }
}
