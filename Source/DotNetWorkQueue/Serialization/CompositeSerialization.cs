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
namespace DotNetWorkQueue.Serialization
{
    /// <summary>
    /// A Composite serialization wrapper that provides access to both the user and internal serialization
    /// </summary>
    public class CompositeSerialization : ICompositeSerialization
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompositeSerialization"/> class.
        /// </summary>
        /// <param name="internalSerializer">The internal serializer.</param>
        /// <param name="serializer">The serializer.</param>
        public CompositeSerialization(IInternalSerializer internalSerializer,
            ASerializer serializer)
        {
            InternalSerializer = internalSerializer;
            Serializer = serializer;
        }

        /// <summary>
        /// Gets the internal serializer.
        /// </summary>
        /// <value>
        /// The internal serializer.
        /// </value>
        public IInternalSerializer InternalSerializer { get;  }

        /// <summary>
        /// Gets the message serializer.
        /// </summary>
        /// <value>
        /// The message serializer.
        /// </value>
        public ASerializer Serializer { get;  }
    }
}
