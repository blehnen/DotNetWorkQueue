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
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.LiteDb.Basic.CommandHandler
{
    internal class DashboardUpdateMessageBodyCommandHandler : ICommandHandlerWithOutput<DashboardUpdateMessageBodyCommand, long>
    {
        private readonly LiteDbConnectionManager _connectionInformation;
        private readonly TableNameHelper _tableNameHelper;

        public DashboardUpdateMessageBodyCommandHandler(
            LiteDbConnectionManager connectionInformation,
            TableNameHelper tableNameHelper)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => tableNameHelper, tableNameHelper);

            _connectionInformation = connectionInformation;
            _tableNameHelper = tableNameHelper;
        }

        public long Handle(DashboardUpdateMessageBodyCommand command)
        {
            var id = int.Parse(command.MessageId);

            using (var db = _connectionInformation.GetDatabase())
            {
                var col = db.Database.GetCollection<Schema.QueueTable>(_tableNameHelper.QueueName);
                var record = col.Query()
                    .Where(x => x.Id == id)
                    .FirstOrDefault();

                if (record == null)
                    return 0;

                record.Body = command.Body;
                record.Headers = command.Headers;
                col.Update(record);

                return 1;
            }
        }
    }
}
