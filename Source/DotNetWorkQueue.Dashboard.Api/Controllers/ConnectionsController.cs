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
    /// API controller for managing dashboard connections.
    /// </summary>
    [ApiController]
    [Route("api/v1/dashboard/connections")]
    [Produces("application/json")]
    public class ConnectionsController : ControllerBase
    {
        private readonly IDashboardService _service;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionsController"/> class.
        /// </summary>
        public ConnectionsController(IDashboardService service)
        {
            _service = service;
        }

        /// <summary>
        /// Gets all registered connections.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<ConnectionResponse>), 200)]
        public IActionResult GetConnections()
        {
            return Ok(_service.GetConnections());
        }

        /// <summary>
        /// Gets all queues for a specific connection.
        /// </summary>
        [HttpGet("{connectionId:guid}/queues")]
        [ProducesResponseType(typeof(IReadOnlyList<QueueInfoResponse>), 200)]
        [ProducesResponseType(404)]
        public IActionResult GetQueues(Guid connectionId)
        {
            return Ok(_service.GetQueues(connectionId));
        }

        /// <summary>
        /// Gets all scheduled jobs for a connection.
        /// </summary>
        [HttpGet("{connectionId:guid}/jobs")]
        [ProducesResponseType(typeof(IReadOnlyList<JobResponse>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetJobs(Guid connectionId)
        {
            return Ok(await _service.GetJobsByConnectionAsync(connectionId));
        }
    }
}
