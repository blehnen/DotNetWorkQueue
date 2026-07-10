// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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

namespace DotNetWorkQueue.Exceptions
{
    /// <summary>
    /// An error has occurred while compiling code
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.Exceptions.DotNetWorkQueueException" />
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
        /// <summary>
        /// Gets the code that was being compiled.
        /// </summary>
        /// <value>
        /// The compiled code.
        /// </value>
        public string CompileCode { get; }
    }
}
