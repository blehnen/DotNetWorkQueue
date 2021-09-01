﻿// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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
using OpenTelemetry.Trace;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// No Op worker notification
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IWorkerNotification" />
    public class WorkerNotificationNoOp : IWorkerNotification
    {
        /// <inheritdoc/>
        public IHeaders HeaderNames => null;

        /// <inheritdoc/>
        public IWorkerHeartBeatNotification HeartBeat
        {
            get => null;
            set
            {
                
            }
        }

        /// <inheritdoc/>
        public ILogger Log => null;

        /// <inheritdoc/>
        public IMetrics Metrics => null;

        /// <inheritdoc/>
        public ActivitySource Tracer => null;

        /// <inheritdoc/>
        public bool TransportSupportsRollback => false;

        /// <inheritdoc/>
        public ICancelWork WorkerStopping
        {
            get => null;
            set
            {
            }
        }
    }
}
