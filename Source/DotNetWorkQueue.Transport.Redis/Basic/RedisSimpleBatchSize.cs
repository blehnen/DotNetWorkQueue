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
namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// A very simple class that determines how big a batch should be when sending a batch of messages
    /// </summary>
    internal class RedisSimpleBatchSize : ISendBatchSize
    {
        /// <summary>
        /// Determines the size of the send message batch, based on the total number of messages to be sent
        /// </summary>
        /// <param name="messageCount">The message count.</param>
        /// <returns></returns>
        public int BatchSize(int messageCount)
        {
            if (messageCount <= 50)
            {
                return messageCount;
            }
            if (messageCount < 512)
            {
                return messageCount / 2;
            }
            return 256;
        }
    }
}
