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

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Adds system standard headers to out going messages
    /// </summary>
    public class AddStandardMessageHeaders
    {
        private readonly IHeaders _headers;
        private readonly IGetFirstMessageDeliveryTime _getFirstMessageDeliveryTime;

        /// <summary>
        /// Initializes a new instance of the <see cref="AddStandardMessageHeaders"/> class.
        /// </summary>
        /// <param name="headers">The headers.</param>
        /// <param name="getFirstMessageDeliveryTime">The get first message delivery time.</param>
        public AddStandardMessageHeaders(IHeaders headers,
            IGetFirstMessageDeliveryTime getFirstMessageDeliveryTime)
        {
            _headers = headers;
            _getFirstMessageDeliveryTime = getFirstMessageDeliveryTime;
        }
        /// <summary>
        /// Adds the headers.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="data">The data.</param>
        public void AddHeaders (IMessage message, IAdditionalMessageData data)
        {
            message.SetHeader(_headers.StandardHeaders.FirstPossibleDeliveryDate, new ValueTypeWrapper<DateTime>(_getFirstMessageDeliveryTime.GetTime(message, data)));
        }
    }
}
