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
    /// A message rollback notification
    /// </summary>
    public class RollBackNotification : ABaseNotification
    {
        /// <summary>
        /// A message rollback notification.
        /// </summary>
        /// <param name="id">The message id.</param>
        /// <param name="correlationId">The correlation id.</param>
        /// <param name="headers">The message headers.</param>
        /// <param name="error">The message error.</param>
        public RollBackNotification(IMessageId id, ICorrelationId correlationId, IReadOnlyDictionary<string, object> headers, Exception error) : base(id, correlationId, headers)
        {
            Error = error;
        }

        /// <summary>
        /// The error that triggered the rollback.
        /// </summary>
        public Exception Error { get; }
    }
}
