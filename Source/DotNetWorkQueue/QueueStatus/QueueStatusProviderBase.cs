// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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

namespace DotNetWorkQueue.QueueStatus
{
    /// <summary>
    /// Base queue status provider
    /// </summary>
    public abstract class QueueStatusProviderBase : IQueueStatusProvider
    {
        /// <summary>
        /// The time factory
        /// </summary>
        protected readonly IGetTimeFactory TimeFactory;
        /// <summary>
        /// The connection information
        /// </summary>
        protected readonly IConnectionInformation ConnectionInformation;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueStatusProviderBase" /> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        /// <param name="getTimeFactory">The get time factory.</param>
        protected QueueStatusProviderBase(IConnectionInformation connectionInformation,
            IGetTimeFactory getTimeFactory)
        {
            TimeFactory = getTimeFactory;
            ConnectionInformation = connectionInformation;
            Name = connectionInformation.QueueName;
            Server = connectionInformation.Server;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; }
        /// <summary>
        /// Gets the server.
        /// </summary>
        /// <value>
        /// The server.
        /// </value>
        public string Server { get; }

        /// <summary>
        /// Gets the current queue status / information
        /// </summary>
        /// <value>
        /// The current.
        /// </value>
        public IQueueInformation Current => BuildStatus();

        /// <summary>
        /// Gets the last error that occurred, if any.
        /// </summary>
        /// <value>
        /// The error.
        /// </value>
        public Exception Error { get; private set; }

        /// <summary>
        /// Handles custom URL paths
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        /// <remarks>Optional. Return null to indicate that this path is not handled by this provider. Otherwise, return a serializable object</remarks>
        public virtual object HandlePath(string path)
        {
            return null;
        }

        /// <summary>
        /// Sets the error.
        /// </summary>
        /// <param name="error">The error.</param>
        protected void SetError(Exception error)
        {
            Error = error;
        }

        /// <summary>
        /// Builds the status.
        /// </summary>
        /// <returns></returns>
        protected IQueueInformation BuildStatus()
        {
            var date = TimeFactory.Create();
            var dateType = date?.GetType().Name ?? "None";
            var dateTime = date?.GetCurrentUtcDate() ?? DateTime.MinValue;
            try
            {
                return new QueueInformation(ConnectionInformation.QueueName, ConnectionInformation.Server, dateTime, dateType, GetData());
            }
            catch (Exception error)
            {
                return new QueueInformationError(ConnectionInformation.QueueName, ConnectionInformation.Server, error);
            }
        }

        /// <summary>
        /// Gets the transport specific data.
        /// </summary>
        /// <returns></returns>
        protected abstract IEnumerable<SystemEntry> GetData();
    }
}
