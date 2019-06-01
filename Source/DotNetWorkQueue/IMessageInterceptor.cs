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
using System;
using System.Collections.Generic;
using DotNetWorkQueue.Interceptors;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Defines a message interceptor. The interceptor can perform transformations on the raw message serialization both when serializing 
    /// and de-serializing a message.
    /// <remarks>For instance, this can be used to compress or encrypt the message in the transport</remarks>
    /// <seealso cref="GZipMessageInterceptor"/>
    /// <seealso cref="TripleDesMessageInterceptor"/>
    /// </summary>
    public interface IMessageInterceptor
    {
        /// <summary>
        /// Runs the interceptor on the input and returns the output as a byte array. Used to serialize a message stream.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="headers">the message headers</param>
        /// <returns></returns>
        MessageInterceptorResult MessageToBytes(byte[] input, IReadOnlyDictionary<string, object> headers);
        /// <summary>
        /// Runs the interceptor on the input and returns the output as a byte array. Used to re-construct a message stream.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="headers">the message headers</param>
        /// <returns></returns>
        byte[] BytesToMessage(byte[] input, IReadOnlyDictionary<string, object> headers);

        /// <summary>
        /// Gets the display name for logging or display purposes
        /// </summary>
        /// <value>
        /// The display name.
        /// </value>
        string DisplayName { get; }

        /// <summary>
        /// The base type of the interceptor; used for re-creation
        /// </summary>
        /// <value>
        /// The type of the base.
        /// </value>
        Type BaseType { get; }
    }

    /// <summary>
    /// The result of an interception
    /// </summary>
    public class MessageInterceptorResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageInterceptorResult"/> class.
        /// </summary>
        /// <param name="output">The output.</param>
        /// <param name="addToGraph">if set to <c>true</c> [the interceptor has injected itself].</param>
        /// <param name="interceptorType">Type of the interceptor.</param>
        public MessageInterceptorResult(byte[] output, bool addToGraph, Type interceptorType)
        {
            Output = output;
            AddToGraph = addToGraph;
            BaseType = interceptorType;
        }
        /// <summary>
        /// Gets or sets the output.
        /// </summary>
        /// <value>
        /// The output.
        /// </value>
        public byte[] Output { get; }
        /// <summary>
        /// Returns true if the interceptor injected itself
        /// </summary>
        /// <value>
        /// True or false
        /// </value>
        public bool AddToGraph { get; }
        /// <summary>
        /// Gets or sets the base type of the interceptor
        /// </summary>
        /// <value>
        /// Base type of the interceptor
        /// </value>
        public Type BaseType { get; }
    }
}
