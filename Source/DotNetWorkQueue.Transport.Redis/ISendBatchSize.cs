// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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
namespace DotNetWorkQueue.Transport.Redis
{
    /// <summary>
    /// Determines the size of the send message batch, based on the total number of messages to be sent
    /// </summary>
    public interface ISendBatchSize
    {
        /// <summary>
        /// Determines the size of the send message batch, based on the total number of messages to be sent
        /// </summary>
        /// <param name="messageCount">The message count.</param>
        /// <returns></returns>
        int BatchSize(int messageCount);
    }
}
