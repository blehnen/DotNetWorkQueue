// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
namespace DotNetWorkQueue.Queue
{
    /// <summary>
    /// Exception event args for message processing
    /// </summary>
    public class MessageErrorEventArgs: EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageErrorEventArgs"/> class.
        /// </summary>
        /// <param name="error">The error.</param>
        public MessageErrorEventArgs(Exception error)
        {
            Error = error;
        }
        /// <summary>
        /// Gets the error.
        /// </summary>
        /// <value>
        /// The error.
        /// </value>
        public Exception Error { get; }
    }
}
