// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
using System.Data;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.SQLite.Basic.CommandSetup;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.Transport.RelationalDatabase.ISetupCommand" />
    public class SetupCommand : ISetupCommand
    {
        private readonly IGetTimeFactory _getTimeFactory;
        private readonly QueueConsumerConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetupCommand" /> class.
        /// </summary>
        /// <param name="getTimeFactory">The get time factory.</param>
        /// <param name="configuration">The configuration.</param>
        public SetupCommand(IGetTimeFactory getTimeFactory,
            QueueConsumerConfiguration configuration)
        {
            Guard.NotNull(() => getTimeFactory, getTimeFactory);
            Guard.NotNull(() => configuration, configuration);
            _getTimeFactory = getTimeFactory;
            _configuration = configuration;
        }

        /// <summary>
        /// Setup the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <param name="type">The type.</param>
        /// <param name="commandParams">The command parameters.</param>
        /// <exception cref="NotImplementedException"></exception>
        public void Setup(IDbCommand command, CommandStringTypes type, object commandParams)
        {
            ISetupCommand setup;
            switch (type)
            {
                case CommandStringTypes.ResetHeartbeat:
                    setup = new ResetHeartbeatSetup();
                    break;
                case CommandStringTypes.FindExpiredRecordsToDelete:
                case CommandStringTypes.FindExpiredRecordsWithStatusToDelete:
                    setup = new FindExpiredMessagesToDeleteSetup(_getTimeFactory);
                    break;
                case CommandStringTypes.GetHeartBeatExpiredMessageIds:
                    setup = new FindRecordsToResetByHeartBeatSetup(_configuration, _getTimeFactory);
                    break;
                default:
                    throw new NotImplementedException();
            }
            setup?.Setup(command, type, commandParams);
        }
    }
}
