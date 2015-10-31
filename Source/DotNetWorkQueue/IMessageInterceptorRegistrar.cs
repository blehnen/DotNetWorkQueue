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

using System;
using System.Collections.Generic;
namespace DotNetWorkQueue
{
    /// <summary>
    /// Defines a class that can register <see cref="IMessageInterceptor"/> for usage  
    /// </summary>
    public interface IMessageInterceptorRegistrar 
    {
        /// <summary>
        /// Runs the interceptor on the input and returns the output as a byte array. Used to serialize a message stream.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        MessageInterceptorsResult MessageToBytes(byte[] input);
        /// <summary>
        /// Runs the interceptor on the input and returns the output as a byte array. Used to re-construct a message stream.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="graph">The graph.</param>
        /// <returns></returns>
        byte[] BytesToMessage(byte[] input, MessageInterceptorsGraph graph);
    }

    /// <summary>
    /// The result of all message interceptions
    /// </summary>
    public class MessageInterceptorsResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageInterceptorsResult"/> class.
        /// </summary>
        public MessageInterceptorsResult()
        {
            Graph = new MessageInterceptorsGraph();
        }
        /// <summary>
        /// Gets or sets the output.
        /// </summary>
        /// <value>
        /// The output.
        /// </value>
        public byte[] Output { get; set; }
        /// <summary>
        /// Gets or sets the graph.
        /// </summary>
        /// <value>
        /// The graph.
        /// </value>
        public MessageInterceptorsGraph Graph { get; private set; }
    }

    /// <summary>
    /// Tracks which message interceptors have been used on a message
    /// </summary>
    public class MessageInterceptorsGraph
    {
        private readonly List<Type> _alTypes = new List<Type>(); 
        /// <summary>
        /// Adds the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        public void Add(Type type)
        {
            _alTypes.Add(type);
        }

        /// <summary>
        /// Gets the message interceptor types.
        /// </summary>
        /// <value>
        /// The types.
        /// </value>
        public IEnumerable<Type> Types => _alTypes;
    }
}
