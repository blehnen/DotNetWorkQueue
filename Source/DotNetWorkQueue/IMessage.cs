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
using System.Collections.Generic;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Defines a message created by a producer queue
    /// </summary>
    public interface IMessage
    {
        /// <summary>
        /// Gets the body of the message.
        /// </summary>
        /// <value>
        /// The body.
        /// </value>
        /// <remarks>Internally user messages are treated as dynamic, as we don't care what type it is</remarks>
        dynamic Body { get; }

        /// <summary>
        /// Gets the headers.
        /// </summary>
        /// <value>
        /// The headers.
        /// </value>
        /// <remarks>If possible use <seealso cref="GetHeader{THeader}"/> and <seealso cref="SetHeader{THeader}"/> to get/set data in a type safe manner</remarks>
        IDictionary<string, object> Headers { get; set; }

        /// <summary>
        /// Returns data set by <see cref="SetHeader{THeader}"/> 
        /// </summary>
        /// <typeparam name="THeader">data type</typeparam>
        /// <param name="property">The property.</param>
        /// <returns></returns>
        THeader GetHeader<THeader>(IMessageContextData<THeader> property)
            where THeader : class;

        /// <summary>
        /// Allows for attaching data to messages outside of the body.
        /// </summary>
        /// <typeparam name="THeader">data type</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        void SetHeader<THeader>(IMessageContextData<THeader> property, THeader value)
            where THeader : class;

        /// <summary>
        /// Returns data set by <see cref="SetHeader{THeader}"/> 
        /// </summary>
        /// <typeparam name="THeader">data type</typeparam>
        /// <param name="property">The property.</param>
        /// <returns></returns>
        THeader GetInternalHeader<THeader>(IMessageContextData<THeader> property)
            where THeader : class;

        /// <summary>
        /// Sets an internal header for access by other parts of the queue. Will not be serialized by the transport.
        /// </summary>
        /// <remarks>Data that needs to be persistent should be set via <see cref="SetHeader{THeader}"/> </remarks>
        /// <typeparam name="THeader">data type</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        void SetInternalHeader<THeader>(IMessageContextData<THeader> property, THeader value)
            where THeader : class;
    }
}
