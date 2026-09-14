// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;
using LiteDB;

namespace DotNetWorkQueue.Transport.LiteDb.Basic.CommandHandler
{
    /// <inheritdoc />
    /// <summary>
    /// Sends a heart beat for a queue record
    /// </summary>
    internal class SendHeartBeatCommandHandler : ICommandHandlerWithOutput<SendHeartBeatCommand<int>, DateTime?>
    {
        private readonly LiteDbConnectionManager _connectionInformation;
        private readonly TableNameHelper _tableNameHelper;
        //LiteDB is embedded, so the configured provider is normally the local clock anyway - but
        //these values are stored and compared against each other, so they take whichever clock
        //the queue was told to use rather than going around it
        private readonly IGetTime _getTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="SendHeartBeatCommandHandler" /> class.
        /// </summary>
        public SendHeartBeatCommandHandler(LiteDbConnectionManager connectionInformation,
            TableNameHelper tableNameHelper,
            IGetTimeFactory getTimeFactory)
        {
            Guard.NotNull(getTimeFactory);
            _getTime = getTimeFactory.Create();
            Guard.NotNull(connectionInformation);
            Guard.NotNull(tableNameHelper);

            _connectionInformation = connectionInformation;
            _tableNameHelper = tableNameHelper;
        }

        /// <inheritdoc />
        public DateTime? Handle(SendHeartBeatCommand<int> command)
        {
            using (var db = _connectionInformation.GetDatabase())
            {
                db.Database.BeginTrans();
                try
                {
                    var col = db.Database.GetCollection<Schema.MetaDataTable>(_tableNameHelper.MetaDataName);

                    var results = col.Query()
                        .Where(x => x.QueueId == command.QueueId)
                        .Limit(1)
                        .ToList();

                    DateTime? date = null;
                    //The second test is the ownership check the relational transports make in SQL: beat
                    //only if the stored heartbeat is still the one this worker wrote. Once the monitor
                    //has reset the message and another worker has taken it, the value differs and this
                    //worker is told its claim is gone rather than refreshing somebody else's
                    //(GitHub #328). A null expectation is the first beat, which does not constrain -
                    //the de-queue wrote a heartbeat this worker never saw.
                    if (results.Count == 1 &&
                        (!command.PreviousHeartBeat.HasValue || results[0].HeartBeat == command.PreviousHeartBeat))
                    {
                        var record = results[0];
                        //Truncated to the precision LiteDb actually stores. A BSON date keeps
                        //milliseconds, so handing the caller a tick-precision value would hand it
                        //something that was never written - and the next beat, which compares what it
                        //last wrote against the stored value, would never match again.
                        date = TruncateToStoredPrecision(_getTime.GetCurrentUtcDate());
                        record.HeartBeat = date;
                        col.Update(record);
                    }

                    db.Database.Commit();
                    return date;
                }
                catch
                {
                    db.Database.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// The value as LiteDb will store it, so that what a caller is told it wrote is what a later
        /// read returns.
        /// </summary>
        private static DateTime TruncateToStoredPrecision(DateTime value) =>
            new DateTime(value.Ticks - value.Ticks % TimeSpan.TicksPerMillisecond, value.Kind);
    }
}
