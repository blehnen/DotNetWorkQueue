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
using DotNetWorkQueue.Transport.Redis.Basic.Command;
using OpenTracing;

namespace DotNetWorkQueue.Transport.Redis.Trace
{
    /// <summary>
    /// Tracing extensions
    /// </summary>
    public static class TraceExtensions
    {
        /// <summary>
        /// Adds tags to the span from the data
        /// </summary>
        /// <param name="span">The span.</param>
        /// <param name="data">The data.</param>
        public static void Add(this ISpan span, IAdditionalMessageData data)
        {
            var delay = data.GetDelay();
            if (delay.HasValue)
                span.SetTag("MessageDelay",
                    delay.Value.ToString());

            var expiration = data.GetExpiration();
            if (expiration.HasValue)
                span.SetTag("MessageExpiration",
                    expiration.Value.ToString());
        }
    }
}
