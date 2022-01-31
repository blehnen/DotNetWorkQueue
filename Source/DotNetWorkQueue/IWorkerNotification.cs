﻿// ---------------------------------------------------------------------
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

using System.Diagnostics;
using DotNetWorkQueue.Logging;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Trace;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Provides status to the user code that is handling message processing.
    /// </summary>
    public interface IWorkerNotification
    {
        /// <summary>
        /// Provides status on if the queue is shutting down.
        /// </summary>
        ICancelWork WorkerStopping { get; set; }
        /// <summary>
        /// Gets the header names.
        /// </summary>
        /// <value>
        /// The header names.
        /// </value>
        IHeaders HeaderNames { get; }
        /// <summary>
        /// The heart beat status.
        /// </summary>
        /// <value>
        /// The heart beat status.
        /// </value>
        IWorkerHeartBeatNotification HeartBeat { get; set; }
        /// <summary>
        /// If true, the transport being used supports rolling back the de-queue operation.
        /// </summary>
        /// <value>
        /// <c>true</c> if [transport supports rollback]; otherwise, <c>false</c>.
        /// </value>
        /// <remarks>
        /// If this value is false, canceling an in progress operation may result in data loss. 
        /// If using a transport not configured for rollback, re-queuing or saving state is up to user code.
        /// </remarks>
        bool TransportSupportsRollback { get; }
        /// <summary>
        /// An instance of the logging class
        /// </summary>
        /// <value>
        /// The log.
        /// </value>
        ILogger Log { get; }
        /// <summary>
        /// Allows logging of metrics
        /// </summary>
        /// <value>
        /// The metrics.
        /// </value>
        IMetrics Metrics { get; }

        /// <summary>
        /// Allows trace logging
        /// </summary>
        ActivitySource Tracer { get; }
    }
}
