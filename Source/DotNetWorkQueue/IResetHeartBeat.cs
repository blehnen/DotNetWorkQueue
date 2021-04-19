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
using System;
using System.Collections.Generic;
using System.Threading;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Used to find and reset work items that are out of the heart beat window
    /// </summary>
    public interface IResetHeartBeat
    {
        /// <summary>
        /// Used to find and reset work items that are out of the heart beat window
        /// </summary>
        /// <param name="cancelToken">The cancel token. When set, stop processing as soon as possible</param>
        List<ResetHeartBeatOutput> Reset(CancellationToken cancelToken);
    }

    /// <summary>
    /// Output data for resetting a heartbeat
    /// </summary>
    public class ResetHeartBeatOutput
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResetHeartBeatOutput"/> class.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="headers">The headers.</param>
        /// <param name="approximateResetTimeStart">The approximate reset time start.</param>
        /// <param name="approximateResetTimeEnd">The approximate reset time end.</param>
        public ResetHeartBeatOutput(IMessageId messageId, IReadOnlyDictionary<string, object> headers,
            DateTime approximateResetTimeStart, DateTime approximateResetTimeEnd)
        {
            MessageId = messageId;
            Headers = headers;
            ApproximateResetTimeStart = approximateResetTimeStart;
            ApproximateResetTimeEnd = approximateResetTimeEnd;
        }
        /// <summary>
        /// Gets the message identifier.
        /// </summary>
        /// <value>
        /// The message identifier.
        /// </value>
        public IMessageId MessageId { get; }
        /// <summary>
        /// Gets the headers.
        /// </summary>
        /// <value>
        /// The headers.
        /// </value>
        public IReadOnlyDictionary<string, object> Headers { get; }
        /// <summary>
        /// Gets the approximate reset time start.
        /// </summary>
        /// <value>
        /// The approximate reset time start.
        /// </value>
        public DateTime ApproximateResetTimeStart { get; }
        /// <summary>
        /// Gets the approximate reset time end.
        /// </summary>
        /// <value>
        /// The approximate reset time end.
        /// </value>
        public DateTime ApproximateResetTimeEnd { get; }
    }
}
