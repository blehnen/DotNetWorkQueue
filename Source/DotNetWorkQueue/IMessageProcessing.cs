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
using DotNetWorkQueue.Queue;

namespace DotNetWorkQueue
{
    /// <summary>
    /// Handles the actual processing of dequeued message
    /// </summary>
    public interface IMessageProcessing
    {
        /// <summary>
        /// Occurs when message processor is idle
        /// </summary>
        event EventHandler Idle;

        /// <summary>
        /// Occurs when message processor is not idle
        /// </summary>
        event EventHandler NotIdle;

        /// <summary>
        /// Fires when an exception occurs inside of user code
        /// </summary>
        event EventHandler<MessageErrorEventArgs> UserException;

        /// <summary>
        /// Fires when an exception occurs outside of user code
        /// </summary>
        event EventHandler<MessageErrorEventArgs> SystemException;

        /// <summary>
        /// How many async tasks are currently running
        /// </summary>
        /// <value>
        /// The asynchronous task count.
        /// </value>
        long AsyncTaskCount { get; }

        /// <summary>
        /// Processes a message
        /// </summary>
        void Handle();
    }
}
