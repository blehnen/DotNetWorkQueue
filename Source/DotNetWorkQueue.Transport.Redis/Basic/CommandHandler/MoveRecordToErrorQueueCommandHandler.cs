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
using DotNetWorkQueue.Transport.Redis.Basic.Lua;
namespace DotNetWorkQueue.Transport.Redis.Basic.CommandHandler
{
    /// <summary>
    /// Moves a record to the error queue
    /// </summary>
    internal class MoveRecordToErrorQueueCommandHandler : ICommandHandler<MoveRecordToErrorQueueCommand>
    {
        private readonly ErrorLua _errorLua;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteMessageCommandHandler" /> class.
        /// </summary>
        /// <param name="errorLua">The error lua.</param>
        public MoveRecordToErrorQueueCommandHandler(ErrorLua errorLua)
        {
            Guard.NotNull(() => errorLua, errorLua);
            _errorLua = errorLua;
        }

        /// <summary>
        /// Handles the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        public void Handle(MoveRecordToErrorQueueCommand command)
        {
            _errorLua.Execute(command.QueueId.Id.Value.ToString());
        }
    }
}
