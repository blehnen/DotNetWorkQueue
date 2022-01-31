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
using System.Threading;
using DotNetWorkQueue.Logging;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;
using Microsoft.Extensions.Logging;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <inheritdoc cref="IDelayedProcessingMonitor" />
    internal class RedisDelayedProcessingMonitor : BaseMonitor, IDelayedProcessingMonitor
    {
        #region Constructor
        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisDelayedProcessingMonitor"/> class.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="log">The log.</param>
        /// <param name="options">The options.</param>
        public RedisDelayedProcessingMonitor(IDelayedProcessingAction action, ILogger log, RedisQueueTransportOptions options)
            : base(Guard.NotNull(() => action, action).Run, options.DelayedProcessingConfiguration, log)
        {

        }
        #endregion
    }

    /// <inheritdoc />
    internal class DelayedProcessingAction : IDelayedProcessingAction
    {
        private readonly ICommandHandlerWithOutput<MoveDelayedRecordsCommand, long> _moveRecords;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelayedProcessingAction"/> class.
        /// </summary>
        /// <param name="moveRecords">The move records.</param>
        public DelayedProcessingAction(ICommandHandlerWithOutput<MoveDelayedRecordsCommand, long> moveRecords)
        {
            Guard.NotNull(() => moveRecords, moveRecords);
            _moveRecords = moveRecords;
        }

        /// <inheritdoc />
        public long Run(CancellationToken token)
        {
            var records = _moveRecords.Handle(new MoveDelayedRecordsCommand(token));
            var total = records;
            while (records > 0)
            {
                if (token.IsCancellationRequested)
                    return total;
                records = _moveRecords.Handle(new MoveDelayedRecordsCommand(token));
                total += records;
            }
            return total;
        }
    }
}
