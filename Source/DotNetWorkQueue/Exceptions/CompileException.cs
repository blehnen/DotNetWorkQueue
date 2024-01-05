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
using DotNetWorkQueue.Validation;
using System;
using System.Runtime.Serialization;

namespace DotNetWorkQueue.Exceptions
{
    /// <summary>
    /// An error has occurred while compiling code
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.Exceptions.DotNetWorkQueueException" />
    [Serializable]
    public class CompileException : DotNetWorkQueueException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompileException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="compileCode">The code being compiled.</param>
        public CompileException(string message, string compileCode)
            : base(message)
        {
            CompileCode = compileCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompileException" /> class.
        /// </summary>
        /// <param name="compileCode">The compile code.</param>
        /// <param name="format">The format.</param>
        /// <param name="args">The arguments.</param>
        public CompileException(string compileCode, string format, params object[] args)
            : base(string.Format(format, args))
        {
            CompileCode = compileCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompileException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner.</param>
        /// <param name="compileCode">The compile code.</param>
        public CompileException(string message, Exception inner, string compileCode)
            : base(message, inner)
        {
            CompileCode = compileCode;
        }
#if NETFULL
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
            Guard.NotNull(() => info, info);
            info.AddValue("CompileCode", CompileCode);
            base.GetObjectData(info, context);
        }
#endif
        /// <summary>
        /// Gets the code that was being compiled.
        /// </summary>
        /// <value>
        /// The compiled code.
        /// </value>
        public string CompileCode { get; }
    }
}
