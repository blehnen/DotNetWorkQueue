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
using System;
using System.Collections.Concurrent;

namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Adds system standard headers to out going messages
    /// </summary>
    public class AddStandardMessageHeaders
    {
        private readonly IHeaders _headers;
        private readonly IGetFirstMessageDeliveryTime _getFirstMessageDeliveryTime;
        private readonly ISerializer _serializer;

        /// <summary>Portable type names, keyed by the body type that produced them.</summary>
        private readonly ConcurrentDictionary<Type, string> _portableTypeNames = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="AddStandardMessageHeaders"/> class.
        /// </summary>
        /// <param name="headers">The headers.</param>
        /// <param name="getFirstMessageDeliveryTime">The get first message delivery time.</param>
        /// <param name="serializer">The serializer that will write the message body.</param>
        public AddStandardMessageHeaders(IHeaders headers,
            IGetFirstMessageDeliveryTime getFirstMessageDeliveryTime,
            ISerializer serializer)
        {
            _headers = headers;
            _getFirstMessageDeliveryTime = getFirstMessageDeliveryTime;
            _serializer = serializer;
        }
        /// <summary>
        /// Adds the headers.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="data">The data.</param>
        public void AddHeaders(IMessage message, IAdditionalMessageData data)
        {
            message.SetHeader(_headers.StandardHeaders.FirstPossibleDeliveryDate, new ValueTypeWrapper<DateTime>(_getFirstMessageDeliveryTime.GetTime(message, data)));

            //Record which serializer will write the body, so the consumer can read it back with
            //the same one. Stamped here rather than inside the serializer because every send path
            //- single, batch, async, and the method and LINQ queues through ProducerQueue - runs
            //through this method, so it cannot depend on a transport's ordering of body and header
            //serialization.
            message.SetHeader(_headers.StandardHeaders.SerializerId, _serializer.SerializerId);

            // Record the portable body type for the dashboard.
            // Skip delegate types (method/LINQ queues) — Action<T>/expression types are not meaningful to display.
            var bodyType = ((object)message.Body)?.GetType();
            if (bodyType != null && !typeof(Delegate).IsAssignableFrom(bodyType))
                message.SetHeader(_headers.StandardHeaders.MessageBodyType, GetPortableTypeName(bodyType));
        }

        /// <summary>
        /// Returns "TypeFullName, AssemblySimpleName" — version, culture and public key token are
        /// stripped so the header remains resolvable across assembly version changes.
        /// </summary>
        /// <remarks>
        /// Cached per body type. The name is fixed for the lifetime of the type, but building it calls
        /// <see cref="System.Reflection.Assembly.GetName()"/>, which parses the assembly identity
        /// and allocates an <see cref="System.Reflection.AssemblyName"/> every time - measured at
        /// 205 ns and 520 bytes, paid on every message sent. A queue sends a small, fixed set of
        /// body types, so the dictionary stays small on its own and needs no eviction.
        /// <para>
        /// The cache is per instance rather than static: this class is registered as a singleton
        /// per queue, so an instance field lives exactly as long as the queue does and does not
        /// keep a body type - and the assembly holding it - alive beyond that.
        /// </para>
        /// </remarks>
        private string GetPortableTypeName(Type type)
        {
            return _portableTypeNames.GetOrAdd(type,
                static t => $"{t.FullName}, {t.Assembly.GetName().Name}");
        }
    }
}
