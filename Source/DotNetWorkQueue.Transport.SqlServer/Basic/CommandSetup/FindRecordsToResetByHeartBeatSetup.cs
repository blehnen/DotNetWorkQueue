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
using System.Data.SqlClient;
using DotNetWorkQueue.Configuration;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SqlServer.Basic.CommandSetup
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.Transport.RelationalDatabase.ISetupCommand" />
    public class FindRecordsToResetByHeartBeatSetup : ISetupCommand
    {
        private readonly QueueConsumerConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="FindRecordsToResetByHeartBeatSetup"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public FindRecordsToResetByHeartBeatSetup(QueueConsumerConfiguration configuration)
        {
            Guard.NotNull(() => configuration, configuration);
            _configuration = configuration;
        }
        /// <summary>
        /// Setups the specified input command.
        /// </summary>
        /// <param name="inputCommand">The input command.</param>
        /// <param name="type">The type.</param>
        /// <param name="commandParams">The command parameters.</param>
        public void Setup(IDbCommand inputCommand, CommandStringTypes type, object commandParams)
        {
            var command = (SqlCommand)inputCommand;

            command.Parameters.Add("@Time", SqlDbType.BigInt);
            command.Parameters["@Time"].Value = _configuration.HeartBeat.Time.TotalSeconds;
            command.Parameters.Add("@Status", SqlDbType.Int);
            command.Parameters["@Status"].Value = Convert.ToInt16(QueueStatuses.Processing);
        }
    }
}
