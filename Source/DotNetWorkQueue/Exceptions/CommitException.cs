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

namespace DotNetWorkQueue.Exceptions
{
    /// <summary>
    /// A error has occurred while trying to commit a message
    /// </summary>
    /// <remarks>
    /// This means that the message was processed, but now the transport does not know this.
    /// The same exact message may be sent through for processing again at some point, depending on queue settings.
    /// </remarks>
    [Serializable]
    public class CommitException : DotNetWorkQueueException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CommitException"/> class.
        /// </summary>
        public CommitException() { }
        /// <summary>
        /// Initializes a new instance of the <see cref="CommitException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public CommitException(string message) : base(message) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="CommitException"/> class.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The arguments.</param>
        public CommitException(string format, params object[] args) : base(string.Format(format, args)) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="CommitException"/> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="inner">The inner.</param>
        public CommitException(string message, Exception inner) : base(message, inner) { }
    }
}
