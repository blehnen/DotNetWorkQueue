// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
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

using DotNetWorkQueue.Transport.Redis.Basic.Command;
namespace DotNetWorkQueue.Transport.Redis.Basic.CommandHandler
{
    /// <summary>
    /// Saves the meta data for a message
    /// </summary>
    internal class SaveMetaDataCommandHandler : ICommandHandler<SaveMetaDataCommand>
    {
        private readonly IInternalSerializer _internalSerializer;
        private readonly RedisNames _redisNames;
        private readonly IRedisConnection _connection;

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveMetaDataCommandHandler" /> class.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="redisNames">The redis names.</param>
        /// <param name="internalSerializer">The internal serializer.</param>
        public SaveMetaDataCommandHandler(IRedisConnection connection, 
            RedisNames redisNames, 
            IInternalSerializer internalSerializer)
        {
            Guard.NotNull(() => connection, connection);
            Guard.NotNull(() => redisNames, redisNames);
            Guard.NotNull(() => internalSerializer, internalSerializer);

            _redisNames = redisNames;
            _internalSerializer = internalSerializer;
            _connection = connection;
        }

        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        public void Handle(SaveMetaDataCommand command)
        {
            var db = _connection.Connection.GetDatabase();
            db.HashSet(_redisNames.MetaData, command.Id.Id.Value.ToString(),
                _internalSerializer.ConvertToBytes(command.MetaData));
        }
    }
}
