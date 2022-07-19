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
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.Admin
{
    internal class AdminFunctions: IAdminFunctions
    {
        private readonly RedisNames _names;
        private readonly IRedisConnection _connection;
        public AdminFunctions(IRedisConnection connection, RedisNames names)
        {
            Guard.NotNull(() => connection, connection);
            Guard.NotNull(() => names, names);

            _connection = connection;
            _names = names;
        }
        public long? Count(QueueStatusAdmin? status)
        {
            var db = _connection.Connection.GetDatabase();
            if (status.HasValue)
            {
                switch (status.Value)
                {
                    case QueueStatusAdmin.Processing:
                        return db.HashLength(_names.Working);
                    case QueueStatusAdmin.Waiting:
                        return db.HashLength(_names.MetaData) - db.HashLength(_names.Working);
                }
            }
            return db.HashLength(_names.MetaData);
        }
    }
}
