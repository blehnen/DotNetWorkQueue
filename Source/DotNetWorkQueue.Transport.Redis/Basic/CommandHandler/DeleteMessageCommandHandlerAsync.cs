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
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Redis.Basic.Lua;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.CommandHandler
{
    /// <inheritdoc />
    internal class DeleteMessageCommandHandlerAsync : ICommandHandlerWithOutputAsync<DeleteMessageCommand<string>, bool>
    {
        private readonly DeleteLua _deleteLua;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteMessageCommandHandlerAsync" /> class.
        /// </summary>
        /// <param name="deleteLua">The delete lua.</param>
        public DeleteMessageCommandHandlerAsync(DeleteLua deleteLua)
        {
            Guard.NotNull(deleteLua);
            _deleteLua = deleteLua;
        }

        /// <inheritdoc />
        public async Task<bool> HandleAsync(DeleteMessageCommand<string> command)
        {
            var result = await _deleteLua.ExecuteAsync(command.QueueId).ConfigureAwait(false);
            return result.HasValue && result.Value == 1;
        }
    }
}
