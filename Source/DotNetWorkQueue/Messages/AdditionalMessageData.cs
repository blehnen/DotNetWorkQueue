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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DotNetWorkQueue.Messages
{
    /// <summary>
    /// Defines additional data that can be attached to a user message
    /// </summary>
    public class AdditionalMessageData : IAdditionalMessageData
    {
        private readonly ConcurrentDictionary<string, object> _settings;
        private readonly IDictionary<string, object> _headers;
        /// <summary>
        /// Initializes a new instance of the <see cref="AdditionalMessageData" /> class.
        /// </summary>
        public AdditionalMessageData()
        {
            AdditionalMetaData = new List<IAdditionalMetaData>();
            TraceTags = new Dictionary<string, string>();
            _headers = new Dictionary<string, object>();
            _settings = new ConcurrentDictionary<string, object>();
        }
        /// <summary>
        /// Gets or sets the correlation identifier. Used to optionally track a message through a system.
        /// </summary>
        /// <value>
        /// The correlation identifier.
        /// </value>
        public ICorrelationId CorrelationId { get; set; }

        /// <summary>
        /// Defines data used to route a message to particular consumers
        /// </summary>
        /// <value>
        /// The route.
        /// </value>
        /// <remarks>
        /// Consumers can be set to only pick up messages with specific route(s)
        /// </remarks>
        public string Route { get; set; }

        /// <summary>
        /// Gets the additional meta data defined by the user.
        /// </summary>
        /// <value>
        /// The additional meta data.
        /// </value>
        public List<IAdditionalMetaData> AdditionalMetaData { get; }
        /// <summary>
        /// Gets the headers.
        /// </summary>
        /// <value>
        /// The headers.
        /// </value>
        public IReadOnlyDictionary<string, object> Headers => new ReadOnlyDictionary<string, object>(_headers);

        /// <inheritdoc/>
        public IDictionary<string, string> TraceTags { get; }

        /// <summary>
        /// Returns data set by <see cref="SetHeader{THeader}"/> 
        /// </summary>
        /// <typeparam name="THeader">data type</typeparam>
        /// <param name="itemData">The property.</param>
        /// <returns></returns>
        public THeader GetHeader<THeader>(IMessageContextData<THeader> itemData)
            where THeader : class
        {
            if (!_headers.ContainsKey(itemData.Name))
            {
                _headers[itemData.Name] = itemData.Default;
            }
            return (THeader)_headers[itemData.Name];
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
            _headers[itemData.Name] = value;
        }

        /// <summary>
        /// Sets a setting.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public void SetSetting(string name, object value)
        {
            _settings[name] = value;
        }

        /// <summary>
        /// Tries to get a setting
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// true if the setting was found
        /// </returns>
        public bool TryGetSetting(string name, out object value)
        {
            return _settings.TryGetValue(name, out value);
        }
    }
}
