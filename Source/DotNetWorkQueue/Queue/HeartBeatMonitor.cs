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
using System.Diagnostics.CodeAnalysis;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Used to update the heart beat of a current work item
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "not needed")]
    public class HeartBeatMonitor : BaseMonitor, IHeartBeatMonitor
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="HeartBeatMonitor" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="resetHeartBeat">The reset heart beat module.</param>
        /// <param name="log">The log.</param>
        public HeartBeatMonitor(IHeartBeatConfiguration configuration, IResetHeartBeat resetHeartBeat, ILogger log): base(configuration, Guard.NotNull(() => resetHeartBeat, resetHeartBeat).Reset, log)
        {

        }
        #endregion
    }
}