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
namespace DotNetWorkQueue
{
    /// <summary>
    /// A composite serialization interface. This wraps both the user serializer and the internal serializer. 
    /// </summary>
    public interface ICompositeSerialization
    {
        /// <summary>
        /// Gets the message serializer.
        /// </summary>
        /// <value>
        /// The message serializer.
        /// </value>
        ASerializer Serializer { get; }
        /// <summary>
        /// Gets the internal serializer.
        /// </summary>
        /// <value>
        /// The internal serializer.
        /// </value>
        IInternalSerializer InternalSerializer { get; }
    }
}
