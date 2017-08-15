// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Messages
{
    /// <summary>
    /// Defines additional meta data for a user message
    /// </summary>
    /// <typeparam name="T">Type of the data</typeparam>
    public class AdditionalMetaData<T> : IAdditionalMetaData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AdditionalMetaData{T}"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public AdditionalMetaData(string name, T value)
        {
            Guard.NotNull(() => name, name);
            Name = name;
            Value = value;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public T Value { get; }
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; }

        // Explicit implementation of IAdditionalMetaData.Value
        /// <summary>
        /// Gets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        object IAdditionalMetaData.Value => Value;
    }
}
