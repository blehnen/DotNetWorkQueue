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
namespace DotNetWorkQueue
{
    /// <summary>
    /// A output message for a job queue request
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.IQueueOutputMessage" />
    public interface IJobQueueOutputMessage: IQueueOutputMessage
    {
        /// <summary>
        /// Gets the status.
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        JobQueuedStatus Status { get; }
    }

    /// <summary>
    /// The status of the job queue request
    /// </summary>
    public enum JobQueuedStatus
    {
        /// <summary>
        /// Job was added
        /// </summary>
        Success = 0,
        /// <summary>
        /// Job was already queued and is waiting for processing
        /// </summary>
        AlreadyQueuedWaiting = 1,
        /// <summary>
        /// The job already exists and is currently processing
        /// </summary>
        AlreadyQueuedProcessing = 2,
        /// <summary>
        /// The job was not added
        /// </summary>
        Failed = 3,
        /// <summary>
        /// Job was added; the job already existed but had an error status
        /// </summary>
        RequeuedDueToErrorStatus = 5,
        /// <summary>
        /// The job has already been processed for the indicated schedule time; it will not be re-added.
        /// </summary>
        AlreadyProcessed = 6
    }
}
