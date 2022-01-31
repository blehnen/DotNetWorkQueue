// ---------------------------------------------------------------------
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
using System;
using System.Linq.Expressions;

namespace DotNetWorkQueue.Messages
{
    /// <summary>
    /// A serializer wrapper for a linq expression tree payload
    /// </summary>
    public class MessageExpression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageExpression"/> class.
        /// </summary>
        /// <remarks>
        /// 
        /// Required for serialization
        /// 
        /// NOTE - look at <see cref="PayLoad"/> to determine which property will be non-null.
        /// 
        /// </remarks>
        public MessageExpression()
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageExpression"/> class.
        /// </summary>
        /// <param name="payload">The type of payload.</param>
        /// <param name="expression">The expression.</param>
        public MessageExpression(MessageExpressionPayloads payload, byte[] expression)
        {
            SerializedExpression = expression;
            PayLoad = payload;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageExpression" /> class.
        /// </summary>
        /// <param name="payload">The type of payload.</param>
        /// <param name="method">The method.</param>
        public MessageExpression(MessageExpressionPayloads payload, Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> method)
        {
            Method = method;
            PayLoad = payload;
        }
        /// <summary>
        /// Gets or sets the serialized expression.
        /// </summary>
        /// <value>
        /// The serialized expression.
        /// </value>
        /// <remarks>This is the LINQ expression, converted to a byte array</remarks>
        public byte[] SerializedExpression { get; set; }
        /// <summary>
        /// Gets or sets the method.
        /// </summary>
        /// <value>
        /// The method.
        /// </value>
        public Expression<Action<IReceivedMessage<MessageExpression>, IWorkerNotification>> Method { get; set; }
        /// <summary>
        /// Gets or sets the pay load.
        /// </summary>
        /// <value>
        /// The pay load.
        /// </value>
        public MessageExpressionPayloads PayLoad { get; set; }
    }

    /// <summary>
    /// Possible expression types
    /// </summary>
    public enum MessageExpressionPayloads
    {
        /// <summary>
        /// An action that returns void.
        /// </summary>
        Action,
        /// <summary>
        /// A linq action expression, specified as a string to be compiled.
        /// </summary>
        ActionText,
        /// <summary>
        /// A linq action expression; not serialized and passed around the queues as-is
        /// </summary>
        /// <remarks>Only suitable for the in-memory queue</remarks>
        ActionRaw
    }
}
