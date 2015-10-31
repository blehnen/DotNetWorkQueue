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

using System.Collections.Generic;
using System.Linq;
namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Generates the headers for a message, by adding any user specified headers
    /// </summary>
    public class GenerateMessageHeaders
    {
        private readonly ICorrelationIdFactory _correlationIdFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenerateMessageHeaders" /> class.
        /// </summary>
        /// <param name="correlationIdFactory">The correlation identifier factory.</param>
        public GenerateMessageHeaders(ICorrelationIdFactory correlationIdFactory)
        {
            Guard.NotNull(() => correlationIdFactory, correlationIdFactory);
            _correlationIdFactory = correlationIdFactory;
        }

        /// <summary>
        /// Generates the headers for a message, and sets a correlation ID if the user didn't provide one.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        public Dictionary<string, object> HeaderSetup(IAdditionalMessageData data)
        {
            Guard.NotNull(() => data, data);

            //a correlation ID is required. Verify that we have one.
            if (data.CorrelationId == null || !data.CorrelationId.HasValue)
            {
                data.CorrelationId = _correlationIdFactory.Create();
            }

            Dictionary<string, object> additionalHeaders = null;
            if (data.Headers != null && data.Headers.Count > 0)
            {
                additionalHeaders = data.Headers.ToDictionary(entry => entry.Key,
                                               entry => entry.Value);
            }
            return additionalHeaders;
        }
    }
}
