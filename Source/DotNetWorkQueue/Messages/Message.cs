// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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

namespace DotNetWorkQueue.Messages
{
    /// <summary>
    /// Represents a queued message
    /// </summary>
    public class Message : IMessage
    {
        private readonly IDictionary<string, object> _headersInternal;
        /// <summary>
        /// Initializes a new instance of the <see cref="Message"/> class.
        /// </summary>
        protected Message()
        {
            Headers = new Dictionary<string, object>();
            _headersInternal = new Dictionary<string, object>();
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Message"/> class.
        /// </summary>
        /// <param name="body">The body.</param>
        public Message(dynamic body): this()
        {
            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }
            Body = body;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="Message"/> class.
        /// </summary>
        /// <param name="body">The body.</param>
        /// <param name="additionalHeaders">The additional headers.</param>
        public Message(dynamic body, IDictionary<string, object> additionalHeaders)
        {
            if (body == null)
            {
                throw new ArgumentNullException(nameof(body));
            }
            _headersInternal = new Dictionary<string, object>();
            Body = body;
            Headers = additionalHeaders != null ? new Dictionary<string, object>(additionalHeaders) : new Dictionary<string, object>();
        }

        /// <summary>
        /// Gets the body.
        /// </summary>
        /// <value>
        /// The body.
        /// </value>
        public dynamic Body { get; set; }

        /// <summary>
        /// Gets the headers.
        /// </summary>
        /// <value>
        /// The headers.
        /// </value>
        public IDictionary<string, object> Headers
        {
            get; set; 
        }
        /// <summary>
        /// Returns data set by <see cref="SetHeader{THeader}"/> 
        /// </summary>
        /// <typeparam name="THeader">data type</typeparam>
        /// <param name="itemData">The property.</param>
        /// <returns></returns>
        public THeader GetHeader<THeader>(IMessageContextData<THeader> itemData)
            where THeader : class
        {
            if (!Headers.ContainsKey(itemData.Name))
            {
                Headers[itemData.Name] = itemData.Default;
            }
            return (THeader)Headers[itemData.Name];
        }

        /// <summary>
        /// Allows additional information to be attached to a message, that is not part of the message body.
        /// </summary>
        /// <typeparam name="THeader">data type</typeparam>
        /// <param name="itemData">The property.</param>
        /// <param name="value">The value.</param>
        public void SetHeader<THeader>(IMessageContextData<THeader> itemData, THeader value)
            where THeader : class
        {
            Headers[itemData.Name] = value;
        }

        /// <summary>
        /// Returns data set by <see cref="SetInternalHeader{THeader}" />
        /// </summary>
        /// <typeparam name="THeader">data type</typeparam>
        /// <param name="itemData">The item data.</param>
        /// <returns></returns>
        public THeader GetInternalHeader<THeader>(IMessageContextData<THeader> itemData)
            where THeader : class
        {
            if (!_headersInternal.ContainsKey(itemData.Name))
            {
                _headersInternal[itemData.Name] = itemData.Default;
            }
            return (THeader)_headersInternal[itemData.Name];
        }

        /// <summary>
        /// Sets an internal header for access by other parts of the queue. Will not be serialized by the transport.
        /// </summary>
        /// <typeparam name="THeader">data type</typeparam>
        /// <param name="itemData">The item data.</param>
        /// <param name="value">The value.</param>
        /// <remarks>
        /// Data that needs to be persistent should be set via <see cref="SetHeader{THeader}" />
        /// </remarks>
        public void SetInternalHeader<THeader>(IMessageContextData<THeader> itemData, THeader value)
            where THeader : class
        {
            _headersInternal[itemData.Name] = value;
        }
    }
}
