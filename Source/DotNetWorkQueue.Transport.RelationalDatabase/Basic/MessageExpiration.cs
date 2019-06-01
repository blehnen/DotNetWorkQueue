// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2018 Brian Lehnen
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
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    /// <summary>
    /// Returns the message expiration timespan, if any, for a new message
    /// </summary>
    public static class MessageExpiration
    {
        /// <summary>
        /// Gets the message expiration.
        /// </summary>
        /// <param name="commandSend">The command send.</param>
        /// <param name="getExpirationFromMessageData">The get expiration from message data.</param>
        /// <returns></returns>
        public static TimeSpan GetExpiration(SendMessageCommand commandSend, 
             Func<IAdditionalMessageData, TimeSpan?> getExpirationFromMessageData)
        {
            var expiration = TimeSpan.Zero;
            //check the message expiration
            if (getExpirationFromMessageData(commandSend.MessageData).HasValue)
            {
                // ReSharper disable once PossibleInvalidOperationException
                expiration = getExpirationFromMessageData(commandSend.MessageData).Value;
            }
            return expiration;
        }
    }
}
