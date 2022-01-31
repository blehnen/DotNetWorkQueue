﻿// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
    /// The message that is returned to a consuming queue.
    /// </summary>
    /// <typeparam name="T">the message body type.</typeparam>
    public interface IReceivedMessage<out T>
        where T : class
    {
        /// <summary>
        /// Gets the body of the message.
        /// </summary>
        /// <value>
        /// The body.
        /// </value>
        T Body { get; }
        /// <summary>
        /// Gets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        IMessageId MessageId { get; }
        /// <summary>
        /// Gets the correlation identifier.
        /// </summary>
        /// <value>
        /// The correlation identifier.
        /// </value>
        ICorrelationId CorrelationId { get; }
        /// <summary>
        /// Provides raw read access to the headers
        /// </summary>
        /// <value>
        /// The headers.
        /// </value>
        IReadOnlyDictionary<string, object> Headers { get; }
        /// <summary>
        /// Returns typed data from the headers collection
        /// </summary>
        /// <typeparam name="THeader">data type</typeparam>
        /// <param name="property">The property.</param>
        /// <returns></returns>
        THeader GetHeader<THeader>(IMessageContextData<THeader> property)
            where THeader : class;

        /// <summary>
        /// True if the previous error messages have been loaded into <see cref="PreviousErrors"/>
        /// </summary>
        bool PreviousErrorsLoaded { get; }

        /// <summary>
        /// A list of the previous errors, if any, that have occurred.
        /// </summary>
        /// <remarks>The string is the type of the exception; the int is the count. Items will only be in the collection if the count is > 0</remarks>
        IReadOnlyDictionary<string, int> PreviousErrors { get; }
    }
}
