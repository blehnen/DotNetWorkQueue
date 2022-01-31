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
using System;

namespace DotNetWorkQueue.AppMetrics
{
    /// <inheritdoc />
    internal class TimerContext : ITimerContext
    {
        private App.Metrics.Timer.TimerContext _timerContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimerContext"/> class.
        /// </summary>
        /// <param name="timerContext">The timer context.</param>
        public TimerContext(App.Metrics.Timer.TimerContext timerContext)
        {
            _timerContext = timerContext;
        }

        /// <inheritdoc />
        public TimeSpan Elapsed => _timerContext.Elapsed;

        /// <inheritdoc />
        public void Dispose()
        {
            _timerContext.Dispose();
        }
    }
}
