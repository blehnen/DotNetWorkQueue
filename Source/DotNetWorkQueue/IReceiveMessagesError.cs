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
namespace DotNetWorkQueue
{
    /// <summary>
    /// Interface for handling a message that has thrown an exception from user code.
    /// </summary>
    public interface IReceiveMessagesError
    {
        /// <summary>
        /// Invoked when a message has failed to process.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="context">The context.</param>
        /// <param name="exception">The exception.</param>
        /// <returns>Result of error processing</returns>
        ReceiveMessagesErrorResult MessageFailedProcessing(IReceivedMessageInternal message, IMessageContext context, Exception exception);
    }

    /// <summary>
    /// Indicates the action that the transport performed
    /// </summary>
    public enum ReceiveMessagesErrorResult
    {
        /// <summary>
        /// The transport did not specify what action was taken
        /// </summary>
        NotSpecified = 0,
        /// <summary>
        /// No action could be performed
        /// </summary>
        /// <remarks>For instance, the message ID is not known.</remarks>
        NoActionPossible = 1,
        /// <summary>
        /// The message will be retried
        /// </summary>
        Retry = 2,
        /// <summary>
        /// The message is flagged as being in an error status and will not be retried without intervention
        /// </summary>
        Error = 3,
    }
}
