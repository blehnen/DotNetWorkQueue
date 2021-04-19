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
using System;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Logging
{
    /// <summary>
    /// Log entry
    /// </summary>
    public class LogEntry
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogEntry"/> class.
        /// </summary>
        /// <param name="severity">The severity.</param>
        /// <param name="message">The message.</param>
        /// <param name="exception">The exception.</param>
        /// <exception cref="ArgumentNullException">message</exception>
        /// <exception cref="ArgumentException">empty - message</exception>
        public LogEntry(LoggingEventType severity, string message, Exception exception = null)
        {
            Guard.NotNullOrEmpty(() => message, message);
            Severity = severity;
            Message = message;
            Exception = exception;
        }

        /// <summary>
        /// Gets the severity.
        /// </summary>
        public LoggingEventType Severity { get; }
        /// <summary>
        /// Gets the message.
        /// </summary>
        public string Message { get; }
        /// <summary>
        /// Gets the exception.
        /// </summary>
        public Exception Exception { get; }
    }
}
