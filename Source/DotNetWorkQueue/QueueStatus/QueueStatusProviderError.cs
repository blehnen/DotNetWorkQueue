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

namespace DotNetWorkQueue.QueueStatus
{
    /// <summary>
    /// Holds information about a failed queue status request
    /// </summary>
    /// <remarks>Each request for status will attempt to recover from the error and re-create a good status module
    /// Once a good module is created, this instance will proxy calls to the good module</remarks>
    /// <typeparam name="TTransportInit">The type of the transport initialize.</typeparam>
    internal class QueueStatusProviderError<TTransportInit> : IQueueStatusProvider
        where TTransportInit : ITransportInit, new()
    {
        private readonly QueueContainer<TTransportInit> _status;
        private readonly string _name;
        private readonly string _connection;
        private IQueueStatusProvider _provider;
        private bool _created;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueStatusProviderError{TTransportInit}" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="connection">The connection.</param>
        /// <param name="statusCreator">The status creator.</param>
        /// <param name="error">The error.</param>
        public QueueStatusProviderError(string name, string connection, QueueContainer<TTransportInit> statusCreator, Exception error)
        {
            _name = name;
            _connection = connection;
            _status = statusCreator;
            Error = error;
        }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name => _created ? _provider.Name : _name;
        /// <summary>
        /// Gets or sets the server.
        /// </summary>
        /// <value>
        /// The server.
        /// </value>
        public string Server => _created ? _provider.Server : "error - name is unknown at this time";

        /// <summary>
        /// Handles custom URL paths
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        /// <remarks>Optional. Return null to indicate that this path is not handled by this provider. Otherwise, return a serializable object</remarks>
        public object HandlePath(string path)
        {
            return null;
        }

        /// <summary>
        /// Gets the current queue information
        /// </summary>
        /// <value>
        /// The current queue information
        /// </value>
        public IQueueInformation Current
        {
            get
            {
                if (_created)
                {
                    return _provider.Current;
                }

                try
                {
                    _provider = _status.CreateStatusProvider(_name, _connection);
                    if (_provider.Error != null) //don't keep chaining errors - if we get another instance of us, just return the error
                    {
                        Error = _provider.Error;
                        return new QueueInformationError(_name, "error", _provider.Error);
                    }
                    _created = true;
                }
                catch (Exception error)
                {
                    return new QueueInformationError(_name, "error", error);
                }
                return _provider.Current;
            }
        }

        /// <summary>
        /// Gets or sets the error.
        /// </summary>
        /// <value>
        /// The error.
        /// </value>
        public Exception Error { get; private set; }
    }
}
