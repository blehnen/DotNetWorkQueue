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
using DotNetWorkQueue.Transport.LiteDb.Basic.Command;
using DotNetWorkQueue.Transport.LiteDb.Schema;
using DotNetWorkQueue.Transport.Shared;

namespace DotNetWorkQueue.Transport.LiteDb.Basic.CommandHandler
{
    /// <summary>
    /// Sets the last known execution time of a scheduled job
    /// </summary>
    public class SetJobLastKnownEventCommandHandler : ICommandHandler<SetJobLastKnownEventCommand>
    {
        private readonly TableNameHelper _tableNameHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetJobLastKnownEventCommandHandler"/> class.
        /// </summary>
        /// <param name="tableNameHelper">The table name helper.</param>
        public SetJobLastKnownEventCommandHandler(TableNameHelper tableNameHelper)
        {
            _tableNameHelper = tableNameHelper;
        }

        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        public void Handle(SetJobLastKnownEventCommand command)
        {
            var col = command.Database.GetCollection<Schema.JobsTable>(_tableNameHelper.JobTableName);

            var results = col.Query()
                .Where(x => x.JobName == command.JobName)
                .Limit(1)
                .ToList();

            if (results != null && results.Count == 1)
            { //this job already existed; just update the time stamps to the last run
                results[0].JobScheduledTime = command.JobScheduledTime;
                results[0].JobEventTime = command.JobEventTime;
            }
            else
            { //this is a brand new job entry
                var record = new JobsTable()
                {
                    JobScheduledTime = command.JobScheduledTime,
                    JobEventTime = command.JobEventTime,
                    JobName = command.JobName
                };
                col.Insert(record);
            }
        }
    }
}
