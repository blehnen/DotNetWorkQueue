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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DotNetWorkQueue.Transport.Memory
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public interface IDataStorage: IClear
    {
        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <param name="messageToSend">The message to send.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        Guid SendMessage(IMessage messageToSend, IAdditionalMessageData data);
        /// <summary>
        /// Sends the message asynchronous.
        /// </summary>
        /// <param name="messageToSend">The message to send.</param>
        /// <param name="data">The data.</param>
        /// <returns></returns>
        Task<Guid> SendMessageAsync(IMessage messageToSend, IAdditionalMessageData data);
        /// <summary>
        /// Moves to error queue.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="context">The context.</param>
        void MoveToErrorQueue(Exception exception, Guid id, IMessageContext context);
        /// <summary>
        /// Gets the next message.
        /// </summary>
        /// <param name="routes">The routes.</param>
        /// <returns></returns>
        IReceivedMessageInternal GetNextMessage(List<string> routes);
        /// <summary>
        /// Gets the next message asynchronous.
        /// </summary>
        /// <param name="routes">The routes.</param>
        /// <returns></returns>
        Task<IReceivedMessageInternal> GetNextMessageAsync(List<string> routes);
        /// <summary>
        /// Deletes the message.
        /// </summary>
        /// <param name="id">The identifier.</param>
        void DeleteMessage(Guid id);

        /// <summary>
        /// Used to inform consumers if work has been added to the queue
        /// </summary>
        /// <value>
        /// The signal.
        /// </value>
        AutoResetEvent Signal { get; }

        /// <summary>
        /// Gets the job last known event.
        /// </summary>
        /// <param name="jobName">Name of the job.</param>
        /// <returns></returns>
        DateTimeOffset GetJobLastKnownEvent(string jobName);
        /// <summary>
        /// Deletes the job.
        /// </summary>
        /// <param name="jobName">Name of the job.</param>
        void DeleteJob(string jobName);
        /// <summary>
        /// Does the job exist.
        /// </summary>
        /// <param name="jobName">Name of the job.</param>
        /// <param name="scheduledTime">The scheduled time.</param>
        /// <returns></returns>
        QueueStatuses DoesJobExist(string jobName, DateTimeOffset scheduledTime);

        /// <summary>
        /// Gets the record count.
        /// </summary>
        /// <value>
        /// The record count.
        /// </value>
        long RecordCount { get; }

        /// <summary>
        /// Gets the error count.
        /// </summary>
        /// <value>
        /// The error count.
        /// </value>
        long GetErrorCount();

        /// <summary>
        /// Gets the dequeue count.
        /// </summary>
        /// <returns></returns>
        long GetDequeueCount();
    }
}
