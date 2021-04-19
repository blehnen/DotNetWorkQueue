// ---------------------------------------------------------------------
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
using System;
using System.Collections.Generic;
using System.Threading;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Transport.Shared.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// Resets records that are outside of the heartbeat window
    /// </summary>
    internal class RedisQueueResetHeartBeat: IResetHeartBeat
    {
        private readonly ICommandHandlerWithOutput<ResetHeartBeatCommand<string>, List<ResetHeartBeatOutput>> _commandReset;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisQueueResetHeartBeat"/> class.
        /// </summary>
        /// <param name="commandReset">The command reset.</param>
        public RedisQueueResetHeartBeat(ICommandHandlerWithOutput<ResetHeartBeatCommand<string>, List<ResetHeartBeatOutput>> commandReset)
        {
            Guard.NotNull(() => commandReset, commandReset);
            _commandReset = commandReset;
        }

        /// <summary>
        /// Used to find and reset work items that are out of the heart beat window
        /// </summary>
        /// <param name="cancelToken">The cancel token.</param>
        public List<ResetHeartBeatOutput> Reset(CancellationToken cancelToken)
        {
            var counter = _commandReset.Handle(new ResetHeartBeatCommand<string>(new MessageToReset<string>(string.Empty, DateTime.MinValue, null)));
            var total = new List<ResetHeartBeatOutput>(counter);
            while (counter.Count > 0)
            {
                if (cancelToken.IsCancellationRequested)
                    return total;

                counter = _commandReset.Handle(new ResetHeartBeatCommand<string>(new MessageToReset<string>(string.Empty, DateTime.MinValue, null)));
                total.AddRange(counter);
            }
            return total;
        }
    }
}
