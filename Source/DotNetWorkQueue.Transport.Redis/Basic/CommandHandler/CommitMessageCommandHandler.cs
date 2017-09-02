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

using DotNetWorkQueue.Transport.Redis.Basic.Command;
using DotNetWorkQueue.Transport.Redis.Basic.Lua;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.CommandHandler
{
    /// <inheritdoc />
    internal class CommitMessageCommandHandler : ICommandHandlerWithOutput<CommitMessageCommand, bool>
    {
        private readonly CommitLua _commitLua;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteMessageCommandHandler" /> class.
        /// </summary>
        /// <param name="commitLua">The delete lua.</param>
        public CommitMessageCommandHandler(CommitLua commitLua)
        {
            Guard.NotNull(() => commitLua, commitLua);
            _commitLua = commitLua;
        }

        /// <inheritdoc />
        public bool Handle(CommitMessageCommand command)
        {
            var result = _commitLua.Execute(command.Id.Id.Value.ToString());
            return result.HasValue && result.Value == 1;
        }
    }
}
