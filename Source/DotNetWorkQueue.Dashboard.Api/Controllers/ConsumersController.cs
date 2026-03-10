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
using System.Linq;
using DotNetWorkQueue.Dashboard.Api.Models;
using DotNetWorkQueue.Dashboard.Api.Services;
using DotNetWorkQueue.Dashboard.Api.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace DotNetWorkQueue.Dashboard.Api.Controllers
{
    /// <summary>
    /// API controller for managing consumer registrations and heartbeats.
    /// </summary>
    [ApiController]
    [Route("api/v1/dashboard/consumers")]
    [Produces("application/json")]
    public class ConsumersController : ControllerBase
    {
        private readonly IConsumerRegistry _registry;
        private readonly DashboardOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConsumersController"/> class.
        /// </summary>
        public ConsumersController(IConsumerRegistry registry, DashboardOptions options)
        {
            _registry = registry;
            _options = options;
        }

        /// <summary>
        /// Registers a new consumer with the dashboard.
        /// </summary>
        [HttpPost("register")]
        [ProducesResponseType(typeof(ConsumerRegistrationResponse), 201)]
        [ProducesResponseType(400)]
        public IActionResult Register([FromBody] ConsumerRegistrationRequest request)
        {
            if (!_options.EnableConsumerTracking)
                return NotFound();

            var consumerId = _registry.Register(
                request.QueueName,
                request.ConnectionString,
                request.MachineName,
                request.ProcessId,
                request.FriendlyName);

            var response = new ConsumerRegistrationResponse
            {
                ConsumerId = consumerId,
                HeartbeatIntervalSeconds = _options.ConsumerHeartbeatIntervalSeconds
            };

            return StatusCode(201, response);
        }

        /// <summary>
        /// Sends a heartbeat for a registered consumer.
        /// </summary>
        [HttpPost("heartbeat")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public IActionResult Heartbeat([FromBody] ConsumerHeartbeatRequest request)
        {
            if (!_options.EnableConsumerTracking)
                return NotFound();

            if (_registry.Heartbeat(request.ConsumerId))
                return NoContent();

            return NotFound();
        }

        /// <summary>
        /// Unregisters a consumer from the dashboard.
        /// </summary>
        [HttpDelete("{consumerId:guid}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public IActionResult Unregister(Guid consumerId)
        {
            if (!_options.EnableConsumerTracking)
                return NotFound();

            if (_registry.Unregister(consumerId))
                return NoContent();

            return NotFound();
        }

        /// <summary>
        /// Gets all active consumers, optionally filtered by queue.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<ConsumerInfoResponse>), 200)]
        public IActionResult GetConsumers([FromQuery] Guid? queueId = null)
        {
            if (!_options.EnableConsumerTracking)
                return Ok(Array.Empty<ConsumerInfoResponse>());

            var entries = queueId.HasValue
                ? _registry.GetByQueue(queueId.Value)
                : _registry.GetAll();

            var response = entries.Select(MapToResponse).ToList();
            return Ok(response);
        }

        /// <summary>
        /// Gets consumer counts per dashboard queue.
        /// </summary>
        [HttpGet("count")]
        [ProducesResponseType(typeof(Dictionary<Guid, int>), 200)]
        public IActionResult GetConsumerCounts()
        {
            if (!_options.EnableConsumerTracking)
                return Ok(new Dictionary<Guid, int>());

            return Ok(_registry.GetCountsByQueue());
        }

        private static ConsumerInfoResponse MapToResponse(ConsumerEntry entry)
        {
            return new ConsumerInfoResponse
            {
                ConsumerId = entry.ConsumerId,
                QueueName = entry.QueueName,
                ConnectionString = entry.ConnectionString,
                MachineName = entry.MachineName,
                ProcessId = entry.ProcessId,
                FriendlyName = entry.FriendlyName,
                RegisteredAt = entry.RegisteredAt,
                LastHeartbeat = entry.LastHeartbeat,
                MatchedQueueId = entry.MatchedQueueId
            };
        }
    }
}
