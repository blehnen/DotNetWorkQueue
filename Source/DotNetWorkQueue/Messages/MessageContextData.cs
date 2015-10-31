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

namespace DotNetWorkQueue.Messages
{
    /// <summary>
    /// Generic data attached to a message context
    /// </summary>
    /// <typeparam name="T">Type of data being attached</typeparam>
    public class MessageContextData<T> : IMessageContextData<T>
        where T : class
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageContextData{T}"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="defaultValue">The default value.</param>
        public MessageContextData(string name, T defaultValue)
        {
            Guard.NotNull(() => name, name);
            Name = name;
            Default = defaultValue;
        }
        /// <summary>
        /// The name of the data.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        /// <remarks>
        /// This is generally used as the storage key - make sure it's unique inside of the context instance.
        /// </remarks>
        public string Name{ get; set; }
        /// <summary>
        /// The default value to use if the data has not yet been set when requesting it.
        /// </summary>
        /// <value>
        /// The default.
        /// </value>
        public T Default { get; }
    }
}
