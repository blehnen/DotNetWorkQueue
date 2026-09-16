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
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Shared.Basic.Command
{
    /// <summary>
    /// Sets the error count for a queue record to a known value.
    /// </summary>
    /// <remarks>
    /// The count is absolute rather than an increment, because the write it drives is wrapped in a
    /// retry policy. A relative update replayed after the server had already committed it would count
    /// one real failure twice, and the message would then reach its retry limit early and be moved to
    /// the error queue with attempts still owed to it - silently, since both the original write and the
    /// replay succeeded. Writing the value the caller already read makes the replay a no-op.
    ///
    /// The cost is the opposite case: two workers failing the same message at the same instant both
    /// read the same count and both write the same value, so one failure is not counted and the message
    /// gets an attempt more than it was configured for. That is the safer direction of the two - an
    /// extra attempt still ends at the error queue, where a missing one does not (GitHub #350).
    /// </remarks>
    public class SetErrorCountCommand<T>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SetErrorCountCommand{T}"/> class.
        /// </summary>
        /// <param name="exceptionType">Type of the exception.</param>
        /// <param name="queueId">The queue identifier.</param>
        /// <param name="retryCount">The total number of failures of this exception type, including the one being recorded.</param>
        public SetErrorCountCommand(string exceptionType, T queueId, int retryCount)
        {
            Guard.NotNullOrEmpty(exceptionType);
            Guard.IsValid(retryCount, i => i >= 1,
                "A retry count is a total rather than an amount to add, so it is never below one");

            ExceptionType = exceptionType;
            QueueId = queueId;
            RetryCount = retryCount;
        }
        /// <summary>
        /// Gets the type of the exception.
        /// </summary>
        /// <value>
        /// The type of the exception.
        /// </value>
        public string ExceptionType { get; }
        /// <summary>
        /// Gets the queue identifier.
        /// </summary>
        /// <value>
        /// The queue identifier.
        /// </value>
        public T QueueId { get; }
        /// <summary>
        /// Gets the value the retry count is to be set to.
        /// </summary>
        /// <value>
        /// The total number of failures, not the amount to add.
        /// </value>
        public int RetryCount { get; }
    }
}
