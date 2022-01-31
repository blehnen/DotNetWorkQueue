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
using System.Collections.Generic;
using DotNetWorkQueue.Queue;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Redis.Basic.Lua;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.CommandHandler
{
    /// <inheritdoc />
    internal class ResetHeartBeatCommandHandler : ICommandHandlerWithOutput<ResetHeartBeatCommand<string>, List<ResetHeartBeatOutput>>
    {
        private readonly IHeartBeatConfiguration _configuration;
        private readonly ResetHeartbeatLua _resetHeartbeatLua;
        private readonly IUnixTimeFactory _unixTimeFactory;
        private readonly RedisQueueTransportOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="RollbackMessageCommandHandler" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        /// <param name="resetHeartbeatLua">The reset heartbeat lua.</param>
        /// <param name="unixTimeFactory">The unix time factory.</param>
        /// <param name="options">The options.</param>
        /// <param name="queueContext">The queue context.</param>
        public ResetHeartBeatCommandHandler(IHeartBeatConfiguration configuration,
            ResetHeartbeatLua resetHeartbeatLua,
            IUnixTimeFactory unixTimeFactory,
            RedisQueueTransportOptions options,
            QueueContext queueContext)
        {
            Guard.NotNull(() => configuration, configuration);
            Guard.NotNull(() => resetHeartbeatLua, resetHeartbeatLua);
            Guard.NotNull(() => unixTimeFactory, unixTimeFactory);
            Guard.NotNull(() => options, options);
            Guard.NotNull(() => queueContext, queueContext);

            _configuration = configuration;
            _resetHeartbeatLua = resetHeartbeatLua;
            _unixTimeFactory = unixTimeFactory;
            _options = options;
        }

        /// <inheritdoc />
        public List<ResetHeartBeatOutput> Handle(ResetHeartBeatCommand<string> command)
        {
            return _resetHeartbeatLua.Execute(_unixTimeFactory.Create().GetSubtractDifferenceMilliseconds(_configuration.Time), _options.ResetHeartBeatBatchLimit);
        }
    }
}
