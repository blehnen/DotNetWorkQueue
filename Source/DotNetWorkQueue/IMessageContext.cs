// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2018 Brian Lehnen
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
    /// Defines the context of processing a message from start (de-queue) to end (commit, rollback)
    /// </summary>
    public interface IMessageContext : IDisposable, IIsDisposed
    {
        /// <summary>Sets the message and headers.</summary>
        /// <param name="id">The identifier. Can be null.</param>
        /// <param name="headers">The headers. Can be null.</param>
        void SetMessageAndHeaders(IMessageId id, IReadOnlyDictionary<string, object> headers);

        /// <summary>
        /// Returns data set by <see cref="Set{T}"/> 
        /// </summary>
        /// <typeparam name="T">data type</typeparam>
        /// <param name="property">The property.</param>
        /// <returns></returns>
        T Get<T>(IMessageContextData<T> property)
            where T : class;

        /// <summary>
        /// Allows the transport to attach data to the context.
        /// <remarks>For instance, data can be attached during de-queue, and then re-accessed during commit</remarks>
        /// </summary>
        /// <typeparam name="T">data type</typeparam>
        /// <param name="property">The property.</param>
        /// <param name="value">The value.</param>
        void Set<T>(IMessageContextData<T> property, T value)
            where T : class;

        /// <summary>
        /// Will be raised when it is time to commit the message.
        /// </summary>
        event EventHandler Commit;

        /// <summary>
        /// Will be raised if the message should be rolled back.
        /// </summary>
        event EventHandler Rollback;

        /// <summary>
        /// Will be raised after work is complete
        /// </summary>
        event EventHandler Cleanup;

        /// <summary>
        /// Gets or sets the message identifier.
        /// </summary>
        /// <value>
        /// The message identifier.
        /// </value>
        IMessageId MessageId { get; }

        /// <summary>
        /// Gets the headers.
        /// </summary>
        /// <value>
        /// The headers.
        /// </value>
        /// <remarks>If possible use <seealso cref="GetHeader{THeader}"/> to get data in a type safe manner</remarks>
        IReadOnlyDictionary<string, object> Headers { get; }

        /// <summary>
        /// Returns data from a header property
        /// </summary>
        /// <typeparam name="THeader">data type</typeparam>
        /// <param name="property">The property.</param>
        /// <returns></returns>
        THeader GetHeader<THeader>(IMessageContextData<THeader> property)
            where THeader : class;

        /// <summary>
        /// Explicitly fires the commit event.
        /// </summary>
        void RaiseCommit();

        /// <summary>
        /// Explicitly fires the rollback event.
        /// </summary>
        void RaiseRollback();

        /// <summary>
        /// Worker notification data, such as stop, cancel or heartbeat failures
        /// </summary>
        /// <value>
        /// The worker notification.
        /// </value>
        IWorkerNotification WorkerNotification { get;  }
    }
}
