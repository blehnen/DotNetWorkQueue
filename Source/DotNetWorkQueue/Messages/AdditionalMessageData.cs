// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using System.Threading;

namespace DotNetWorkQueue.Messages
{
    /// <summary>
    /// Defines additional data that can be attached to a user message
    /// </summary>
    /// <remarks>
    /// One of these is built for every message sent - <c>ProducerQueue.Send</c> creates one when
    /// the caller supplies no data - so its constructor is on the hot path of every transport.
    /// The four collections it holds are therefore created on first use rather than eagerly: most
    /// messages set no user headers, no settings, no meta data and no trace tags, and building
    /// them anyway cost 1,832 bytes and 574 ns per message - 57% of everything a send with no
    /// serialization and no I/O allocated - against 72 bytes and 32 ns for this shape. Most of it
    /// was the <see cref="ConcurrentDictionary{TKey,TValue}"/>, which sizes its lock array from
    /// the processor count.
    /// </remarks>
    public class AdditionalMessageData : IAdditionalMessageData
    {
        private ConcurrentDictionary<string, object> _settings;
        private Dictionary<string, object> _headers;
        private List<IAdditionalMetaData> _additionalMetaData;
        private Dictionary<string, string> _traceTags;

        /// <summary>
        /// The read-only view handed out by <see cref="Headers"/>. Cached because the property was
        /// building a new wrapper on every read, and the send path reads it per message.
        /// </summary>
        private HeaderView _headersView;
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
        public List<IAdditionalMetaData> AdditionalMetaData =>
            LazyInitializer.EnsureInitialized(ref _additionalMetaData, static () => new List<IAdditionalMetaData>());
        /// <summary>
        /// Gets the headers.
        /// </summary>
        /// <value>
        /// The headers.
        /// </value>
        public IReadOnlyDictionary<string, object> Headers => _headersView ??= new HeaderView(this);

        /// <inheritdoc/>
        public IDictionary<string, string> TraceTags =>
            LazyInitializer.EnsureInitialized(ref _traceTags, static () => new Dictionary<string, string>());

        /// <summary>
        /// Returns data set by <see cref="SetHeader{THeader}"/> 
        /// </summary>
        /// <typeparam name="THeader">data type</typeparam>
        /// <param name="itemData">The property.</param>
        /// <returns></returns>
        public THeader GetHeader<THeader>(IMessageContextData<THeader> itemData)
            where THeader : class
        {
            var headers = HeaderStore;
            if (!headers.TryGetValue(itemData.Name, out var value))
            {
                value = itemData.Default;
                headers[itemData.Name] = value;
            }
            return (THeader)value;
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
            HeaderStore[itemData.Name] = value;
        }

        /// <summary>
        /// Sets a setting.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public void SetSetting(string name, object value)
        {
            SettingStore[name] = value;
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
            //read on every send - GetJobName asks for "JobName", and the transports ask for a
            //delay - so it must not create the dictionary just to find it empty
            var settings = _settings;
            if (settings != null) return settings.TryGetValue(name, out value);

            value = null;
            return false;
        }

        /// <summary>The header dictionary, created on first write.</summary>
        private Dictionary<string, object> HeaderStore =>
            LazyInitializer.EnsureInitialized(ref _headers, static () => new Dictionary<string, object>());

        /// <summary>The settings dictionary, created on first write.</summary>
        private ConcurrentDictionary<string, object> SettingStore =>
            LazyInitializer.EnsureInitialized(ref _settings, static () => new ConcurrentDictionary<string, object>());

        /// <summary>
        /// A read-only view of <see cref="_headers"/> that reads it each time rather than wrapping
        /// it once.
        /// </summary>
        /// <remarks>
        /// The property used to hand back a fresh <see cref="ReadOnlyDictionary{TKey,TValue}"/>
        /// over the live dictionary on every call, so a caller holding the result saw headers set
        /// afterwards. Wrapping a dictionary that may not exist yet would break that: the wrapper
        /// would be frozen over an empty dictionary that is never the one written to. Reading the
        /// field through this view keeps the old behaviour, while letting the dictionary itself
        /// stay uncreated for the messages - the great majority - that carry no user headers.
        /// </remarks>
        private sealed class HeaderView : IReadOnlyDictionary<string, object>
        {
            private static readonly Dictionary<string, object> None = new Dictionary<string, object>();
            private readonly AdditionalMessageData _owner;

            public HeaderView(AdditionalMessageData owner)
            {
                _owner = owner;
            }

            private Dictionary<string, object> Current => _owner._headers ?? None;

            public object this[string key] => Current[key];
            public IEnumerable<string> Keys => Current.Keys;
            public IEnumerable<object> Values => Current.Values;
            public int Count => Current.Count;
            public bool ContainsKey(string key) => Current.ContainsKey(key);
            public bool TryGetValue(string key, out object value) => Current.TryGetValue(key, out value);

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator() => Current.GetEnumerator();
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
