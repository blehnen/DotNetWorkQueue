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

                    //Truncated to the precision LiteDb actually stores. A BSON date keeps
                    //milliseconds, so handing the caller a tick-precision value would hand it something
                    //that was never written - and the next beat, which asks for the value it last wrote,
                    //would never match again.
                    var date = TruncateToStoredPrecision(_getTime.GetCurrentUtcDate());
                    var queueId = command.QueueId;

                    //The ownership test goes into the query rather than being made here, for two
                    //reasons. Update() writes by document id and would not re-test anything, so reading
                    //first and writing after leaves a reset and re-claim free to land in between - and
                    //LiteDb's transactions do not hold the record against that (see #318, where queue
                    //creation had to be serialised with a lock of our own). And a heartbeat read back
                    //into a POCO comes out with a local Kind, so comparing it here compares shifted
                    //ticks; the engine compares the stored value properly.
                    //
                    //Zero updated is the same answer rows-affected gives on the relational transports:
                    //the claim is no longer this worker's (GitHub #328).
                    var updated = command.PreviousHeartBeat.HasValue
                        ? col.UpdateMany(x => new Schema.MetaDataTable { HeartBeat = date },
                            x => x.QueueId == queueId && x.HeartBeat == command.PreviousHeartBeat.Value)
                        : col.UpdateMany(x => new Schema.MetaDataTable { HeartBeat = date },
                            x => x.QueueId == queueId);

                    db.Database.Commit();
                    return updated == 1 ? date : (DateTime?)null;
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
