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
namespace DotNetWorkQueue
{
    /// <summary>
    /// Allows storing extra data on the message
    /// </summary>
    /// <remarks>Used to store additional message data via headers or to store additional data on message context</remarks>
    /// <typeparam name="T">the type of the context data</typeparam>
    public interface IMessageContextData<out T> 
        where T: class
    {
        /// <summary>
        /// The name of the data. 
        /// </summary>
        /// <remarks>This is generally used as the storage key - make sure it's unique inside of the context instance.</remarks>
        /// <value>
        /// The name.
        /// </value>
        string Name { get; set; }
        /// <summary>
        /// The default value to use if the data has not yet been set when requesting it.
        /// </summary>
        /// <value>
        /// The default.
        /// </value>
        T Default { get; }
    }
}
