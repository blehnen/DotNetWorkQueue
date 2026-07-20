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
using System;
using System.Collections.Generic;

namespace DotNetWorkQueue.Notifications
{
    /// <summary>
    /// A message error
    /// </summary>
    public class ErrorNotification : ABaseNotification
    {
        /// <summary>
        /// Message error
        /// </summary>
        /// <param name="id">Message Id</param>
        /// <param name="correlationId">Correlation Id</param>
        /// <param name="headers">Message headers</param>
        /// <param name="error">The error</param>
        public ErrorNotification(IMessageId id, ICorrelationId correlationId, IReadOnlyDictionary<string, object> headers, Exception error) : base(id, correlationId, headers)
        {
            Error = error;
        }

        /// <summary>
        /// The exception that occurred.
        /// </summary>
        public Exception Error { get; }
    }
}
