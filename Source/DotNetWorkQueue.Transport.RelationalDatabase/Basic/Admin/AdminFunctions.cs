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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.Admin
{
    internal class AdminFunctions: IAdminFunctions
    {
        private readonly IConnectionInformation _connection;
        private readonly IQueryHandler<GetQueueCountQuery, long> _queueCount;

        public AdminFunctions(
            IConnectionInformation connection,
            IQueryHandler<GetQueueCountQuery, long> queueCount)
        {
            Guard.NotNull(() => connection, connection);
            Guard.NotNull(() => queueCount, queueCount);

            _connection = connection;
            _queueCount = queueCount;

        }
        public long? Count(QueueStatusAdmin? status)
        {
            return _queueCount.Handle(new GetQueueCountQuery(_connection.ConnectionString, status));
        }
    }
}
