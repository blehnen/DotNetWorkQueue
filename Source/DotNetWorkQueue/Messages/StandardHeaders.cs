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
using DotNetWorkQueue.Trace;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Messages
{
    /// <summary>
    /// Contains system standard headers 
    /// </summary>
    public class StandardHeaders : IStandardHeaders
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StandardHeaders"/> class.
        /// </summary>
        /// <param name="messageContextDataFactory">The message context data factory.</param>
        public StandardHeaders(IMessageContextDataFactory messageContextDataFactory)
        {
            Guard.NotNull(() => messageContextDataFactory, messageContextDataFactory);
            FirstPossibleDeliveryDate = messageContextDataFactory.Create<ValueTypeWrapper<DateTime>>("Queue-FirstPossibleDeliveryDate", null);
            MessageInterceptorGraph =
                messageContextDataFactory.Create("Queue-MessageInterceptorGraph",
                    new MessageInterceptorsGraph());
            TraceSpan = messageContextDataFactory.Create<DataMappingTextMap>("Queue-TraceSpan", null);
        }

        /// <inheritdoc/>
        public IMessageContextData<ValueTypeWrapper<DateTime>> FirstPossibleDeliveryDate { get; }

        /// <inheritdoc/>
        public IMessageContextData<MessageInterceptorsGraph> MessageInterceptorGraph { get; }

        /// <inheritdoc/>
        public IMessageContextData<DataMappingTextMap> TraceSpan { get; }
    }
}
