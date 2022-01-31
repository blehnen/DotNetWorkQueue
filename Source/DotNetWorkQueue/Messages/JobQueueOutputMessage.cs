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
namespace DotNetWorkQueue.Messages
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.Messages.QueueOutputMessage" />
    /// <seealso cref="DotNetWorkQueue.IJobQueueOutputMessage" />
    public class JobQueueOutputMessage : QueueOutputMessage, IJobQueueOutputMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JobQueueOutputMessage" /> class.
        /// </summary>
        /// <param name="status">The status.</param>
        public JobQueueOutputMessage(JobQueuedStatus status) : base(null, null)
        {
            Status = status;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JobQueueOutputMessage" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="status">The status.</param>
        public JobQueueOutputMessage(IQueueOutputMessage message, JobQueuedStatus status) : base(message.SentMessage, message.SendingException)
        {
            Status = status;
        }

        /// <summary>
        /// Gets the status.
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        public JobQueuedStatus Status { get; }
    }
}
