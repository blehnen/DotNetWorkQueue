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
using System;
using System.Runtime.Serialization;

namespace DotNetWorkQueue.Exceptions
{
    /// <summary>
    /// A poison message has been pulled from a transport. A poison message can be read from the transport, but can't be re-created.
    /// </summary>
    /// <remarks>
    /// When possible, all 'standard' data is included with the exception. Transport specific data is generally not included.
    /// For instance, user defined columns from the SQL server transport are not included.
    /// </remarks>
    [Serializable]
    public class PoisonMessageException: MessageException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageException"/> class.
        /// </summary>
        public PoisonMessageException() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <param name="messagePayload">The raw message payload.</param>
        /// <param name="headerPayload">The raw header payload.</param>
        public PoisonMessageException(string message, IMessageId messageId, ICorrelationId correlationId, byte[] messagePayload, byte[] headerPayload)
            : base(message, messageId, correlationId)
        {
            MessagePayload = messagePayload;
            HeaderPayload = headerPayload;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageException"/> class.
        /// </summary>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <param name="messagePayload">The raw message payload.</param>
        /// <param name="headerPayload">The raw header payload.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The arguments.</param>
        public PoisonMessageException(IMessageId messageId, ICorrelationId correlationId, byte[] messagePayload, byte[] headerPayload, string format, params object[] args)
            : base(string.Format(format, args), messageId, correlationId)
        {
            MessagePayload = messagePayload;
            HeaderPayload = headerPayload;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner.</param>
        /// <param name="messageId">The message identifier.</param>
        /// <param name="correlationId">The correlation identifier.</param>
        /// <param name="messagePayload">The raw message payload.</param>
        /// <param name="headerPayload">The raw header payload.</param>
        public PoisonMessageException(string message, Exception inner, IMessageId messageId, ICorrelationId correlationId, byte[] messagePayload, byte[] headerPayload)
            : base(message, inner, messageId, correlationId)
        {
            MessagePayload = messagePayload;
            HeaderPayload = headerPayload;
        }

        /// <summary>
        /// When overridden in a derived class, sets the <see cref="T:System.Runtime.Serialization.SerializationInfo" /> with information about the exception.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo" /> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="T:System.Runtime.Serialization.StreamingContext" /> that contains contextual information about the source or destination.</param>
        /// <exception cref="System.ArgumentNullException">info</exception>
        /// <PermissionSet>
        ///   <IPermission class="System.Security.Permissions.FileIOPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Read="*AllFiles*" PathDiscovery="*AllFiles*" />
        ///   <IPermission class="System.Security.Permissions.SecurityPermission, mscorlib, Version=2.0.3600.0, Culture=neutral, PublicKeyToken=b77a5c561934e089" version="1" Flags="SerializationFormatter" />
        /// </PermissionSet>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            info.AddValue("MessagePayload", MessagePayload);
            info.AddValue("HeaderPayload", HeaderPayload);

            base.GetObjectData(info, context);
        }

        /// <summary>
        /// The raw bytes of the serialized poison message
        /// </summary>
        /// <value>
        /// The message payload.
        /// </value>
        public byte[] MessagePayload { get; }

        /// <summary>
        /// The raw bytes of the header payload.
        /// </summary>
        /// <value>
        /// The header payload.
        /// </value>
        public byte[] HeaderPayload { get; }
    }
}
