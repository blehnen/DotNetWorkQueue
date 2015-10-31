// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
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
    /// A base class that provides transport creation methods
    /// </summary>
    public interface IQueueCreation: IDisposable, IIsDisposed
    { 
        /// <summary>
        /// Gets the connection information for the queue.
        /// </summary>
        /// <value>
        /// The connection information.
        /// </value>
        IConnectionInformation ConnectionInfo { get;}

        /// <summary>
        /// Returns true if the queue exists in the transport
        /// </summary>
        /// <value>
        ///   <c>true</c> if [queue exists]; otherwise, <c>false</c>.
        /// </value>
        bool QueueExists { get; }

        /// <summary>
        /// Creates the queue if needed.
        /// </summary>
        /// <returns></returns>
        QueueCreationResult CreateQueue();

        /// <summary>
        /// Attempts to delete an existing queue
        /// </summary>
        /// <remarks>May not be supported by all transports. Any data in the queue will be lost.</remarks>
        /// <returns></returns>
        QueueRemoveResult RemoveQueue();
    }
    /// <summary>
    /// The status of the queue creation process
    /// </summary>
    public enum QueueCreationStatus
    {
        /// <summary>
        /// Default status
        /// <remarks>Getting this status would indicate a logic error, as this code should never be returend in standard operations</remarks>
        /// </summary>
        None = 0,
        /// <summary>
        /// The queue already exists; it was not created.
        /// <remarks>
        /// The queue already exists. This error code is returned if module detects that the queue already exists.
        /// This is different than <see cref="AttemptedToCreateAlreadyExists" />
        /// </remarks>
        /// </summary>
        AlreadyExists = 1,
        /// <summary>
        /// The queue has been created
        /// </summary>
        Success = 2,
        /// <summary>
        /// Attempted to create the queue, but it already exists
        /// </summary>
        /// <remarks>
        /// This is returned when the queue does not exist at the start of call, but exists when in the process of creating the queue.
        /// This may indicate a threading or race condition in your code - i.e. multiple producer queues are running and they are all trying to create 
        /// the queue. This return code does not mean that a fatal error has occured.
        /// </remarks>
        AttemptedToCreateAlreadyExists = 3,
        /// <summary>
        /// The queue configuration is invalid
        /// <remarks>Conflicting configuration settings have been set. See the error message for more detail.</remarks>
        /// </summary>
        ConfigurationError = 4,
        /// <summary>
        /// The transport does not need the queue to be pre-created
        /// </summary>
        /// <remarks>Some transports create data structures as needed.</remarks>
        NoOp = 5
    }

    /// <summary>
    /// Status of queue remove operation
    /// </summary>
    public enum QueueRemoveStatus
    {
        /// <summary>
        /// Default status
        /// <remarks>Getting this status would indicate a logic error, as this code should never be returend in standard operations</remarks>
        /// </summary>
        None = 0,
        /// <summary>
        /// Queue does not exist
        /// <remarks>
        /// The queue doesn't exist. Another process may have deleted it already, or it never existed in the first place.
        /// </remarks>
        /// </summary>
        DoesNotExist = 1,
        /// <summary>
        /// The queue has been removed
        /// </summary>
        Success = 2
    }

    /// <summary>
    /// The results of a remove request
    /// </summary>
    public class QueueRemoveResult
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueRemoveResult"/> class.
        /// </summary>
        /// <param name="status">The status.</param>
        public QueueRemoveResult(QueueRemoveStatus status)
        {
            Status = status;
        }
        #endregion

        #region Public Props
        /// <summary>
        /// The status of a request
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        public QueueRemoveStatus Status { get; }
        /// <summary>
        /// Gets a value indicating whether this <see cref="QueueRemoveResult"/> is successful
        /// </summary>
        /// <value>
        ///   <c>true</c> if success; otherwise, <c>false</c>.
        /// </value>
        public bool Success => Status == QueueRemoveStatus.Success;

        #endregion
    }

    /// <summary>
    /// The results of a creation request
    /// </summary>
    public class QueueCreationResult
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueCreationResult"/> class.
        /// </summary>
        /// <param name="status">The status.</param>
        public QueueCreationResult(QueueCreationStatus status) : this(status, string.Empty) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueCreationResult" /> class.
        /// </summary>
        /// <param name="status">The status.</param>
        /// <param name="errorMessage">The error message.</param>
        public QueueCreationResult(QueueCreationStatus status, string errorMessage)
        {
            Status = status;
            ErrorMessage = errorMessage;
        }
        #endregion

        #region Public Props
        /// <summary>
        /// The status of a request
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        public QueueCreationStatus Status { get; }
        /// <summary>
        /// The error message, set if creation failed.
        /// </summary>
        /// <value>
        /// The error message.
        /// </value>
        public string ErrorMessage { get; private set; }

        /// <summary>
        /// Gets a value indicating if the queue was created, or already exists.
        /// </summary>
        /// <remarks>
        /// While <see cref="QueueCreationStatus.AttemptedToCreateAlreadyExists" /> is treated as success, this may indicate a problem in calling code.
        /// </remarks>
        /// <value>
        ///   <c>true</c> if success; otherwise, <c>false</c>.
        /// </value>
        public bool Success => Status == QueueCreationStatus.Success || Status == QueueCreationStatus.AlreadyExists || Status == QueueCreationStatus.AttemptedToCreateAlreadyExists || Status == QueueCreationStatus.NoOp;

        #endregion
    }
}
