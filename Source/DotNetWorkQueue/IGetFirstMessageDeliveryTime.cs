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
using System;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Gets the first possible delivery time for a message
    /// </summary>
    /// <remarks>This is used to track how long a message sat in a queue before being de-queued. Note that retries do not increase the initial enqueue time.</remarks>
    public interface IGetFirstMessageDeliveryTime
    {
        /// <summary>
        /// Gets the first possible delivery time for a message in UTC format.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="data">The additional message data.</param>
        /// <returns></returns>
        DateTime GetTime(IMessage message, IAdditionalMessageData data);
    }
}
