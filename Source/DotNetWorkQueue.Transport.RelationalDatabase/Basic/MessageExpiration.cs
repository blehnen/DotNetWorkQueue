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
using System;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Command;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    public static class MessageExpiration
    {
        public static TimeSpan GetExpiration(SendMessageCommand commandSend, 
            IHeaders headers, Func<IAdditionalMessageData, TimeSpan?> getExpirationFromMessageData)
        {
            //there are three possible locations for a message expiration. The user data and the header / internal headers
            //grab it from the internal header
            var expiration = commandSend.MessageToSend.GetInternalHeader(headers.StandardHeaders.RpcTimeout).Timeout;
            //if the header value is zero, check the message expiration
            if (expiration == TimeSpan.Zero)
            {
                //try the message header
                expiration = commandSend.MessageToSend.GetHeader(headers.StandardHeaders.RpcTimeout).Timeout;
            }
            //if the header value is zero, check the message expiration
            if (expiration == TimeSpan.Zero && getExpirationFromMessageData(commandSend.MessageData).HasValue)
            {
                // ReSharper disable once PossibleInvalidOperationException
                expiration = getExpirationFromMessageData(commandSend.MessageData).Value;
            }
            return expiration;
        }
    }
}
