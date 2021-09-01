// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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
using DotNetWorkQueue.Trace;
using System;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Contains system standard header assessors
    /// </summary>
    public interface IStandardHeaders
    {
        /// <summary>
        /// Gets the first possible delivery date.
        /// </summary>
        /// <value>
        /// The first possible delivery date.
        /// </value>
        /// <remarks>Used to record the first possible date/time a message could be de-queued</remarks>
        IMessageContextData<ValueTypeWrapper<DateTime>> FirstPossibleDeliveryDate { get; }

        /// <summary>
        /// Gets the message interceptor graph.
        /// </summary>
        /// <value>
        /// The message interceptor graph.
        /// </value>
        IMessageContextData<MessageInterceptorsGraph> MessageInterceptorGraph { get; }
    }

    /// <summary>
    /// A wrapper for allowing a value type to be treated as a reference type
    /// </summary>
    /// <typeparam name="T">The value type (normally a strut) to wrap</typeparam>
    public class ValueTypeWrapper<T>
        where T : struct
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTypeWrapper{T}"/> class.
        /// </summary>
        public ValueTypeWrapper()
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTypeWrapper{T}"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public ValueTypeWrapper(T value)
        {
            Value = value;
        }
        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        /// <value>
        /// The value.
        /// </value>
        public T Value { get; set; }
    }
}
