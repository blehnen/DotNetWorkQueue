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
using DotNetWorkQueue.Dashboard.Api.Models;
using DotNetWorkQueue.Dashboard.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DotNetWorkQueue.Dashboard.Api.Controllers
{
    /// <summary>
    /// API controller for per-queue dashboard operations.
    /// </summary>
    [ApiController]
    [Route("api/v1/queues/queues")]
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
        public IActionResult GetStatus(Guid queueId)
        {
            return Ok(_service.GetStatus(queueId));
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
        public IActionResult GetMessages(Guid queueId,
            [FromQuery] int pageIndex = 0,
            [FromQuery] int pageSize = 25,
            [FromQuery] int? status = null)
        {
            if (status.HasValue && !IsValidStatus(status.Value))
                return BadRequest($"Invalid status value '{status.Value}'. Valid values are: 0 (Waiting), 1 (Processing), 2 (Error), 3 (Processed).");

            return Ok(_service.GetMessages(queueId, pageIndex, pageSize, status));
        }

        /// <summary>
        /// Gets the message count, optionally filtered by status.
        /// </summary>
        [HttpGet("{queueId:guid}/messages/count")]
        [ProducesResponseType(typeof(long), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public IActionResult GetMessageCount(Guid queueId, [FromQuery] int? status = null)
        {
            if (status.HasValue && !IsValidStatus(status.Value))
                return BadRequest($"Invalid status value '{status.Value}'. Valid values are: 0 (Waiting), 1 (Processing), 2 (Error), 3 (Processed).");

            return Ok(_service.GetMessageCount(queueId, status));
        }

        /// <summary>
        /// Gets a single message detail by message ID.
        /// </summary>
        [HttpGet("{queueId:guid}/messages/{messageId:long}")]
        [ProducesResponseType(typeof(MessageResponse), 200)]
        [ProducesResponseType(404)]
        public IActionResult GetMessageDetail(Guid queueId, long messageId)
        {
            var result = _service.GetMessageDetail(queueId, messageId);
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
        public IActionResult GetStaleMessages(Guid queueId,
            [FromQuery] int thresholdSeconds = 60,
            [FromQuery] int pageIndex = 0,
            [FromQuery] int pageSize = 25)
        {
            return Ok(_service.GetStaleMessages(queueId, thresholdSeconds, pageIndex, pageSize));
        }

        /// <summary>
        /// Gets a paged list of error messages.
        /// </summary>
        [HttpGet("{queueId:guid}/errors")]
        [ProducesResponseType(typeof(PagedResponse<ErrorMessageResponse>), 200)]
        [ProducesResponseType(404)]
        public IActionResult GetErrors(Guid queueId,
            [FromQuery] int pageIndex = 0,
            [FromQuery] int pageSize = 25)
        {
            return Ok(_service.GetErrors(queueId, pageIndex, pageSize));
        }

        /// <summary>
        /// Gets error retry tracking records for a specific message.
        /// </summary>
        [HttpGet("{queueId:guid}/messages/{messageId:long}/retries")]
        [ProducesResponseType(typeof(IReadOnlyList<ErrorRetryResponse>), 200)]
        [ProducesResponseType(404)]
        public IActionResult GetErrorRetries(Guid queueId, long messageId)
        {
            return Ok(_service.GetErrorRetries(queueId, messageId));
        }

        /// <summary>
        /// Gets queue configuration as JSON.
        /// </summary>
        [HttpGet("{queueId:guid}/configuration")]
        [ProducesResponseType(typeof(ConfigurationResponse), 200)]
        [ProducesResponseType(404)]
        public IActionResult GetConfiguration(Guid queueId)
        {
            return Ok(_service.GetConfiguration(queueId));
        }

        private static bool IsValidStatus(int status)
        {
            return Enum.IsDefined(typeof(QueueStatuses), (short)status);
        }
    }
}
