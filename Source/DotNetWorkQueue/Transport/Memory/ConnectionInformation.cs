// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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
using System.Collections.Generic;
using DotNetWorkQueue.Configuration;

namespace DotNetWorkQueue.Transport.Memory
{
    /// <summary>
    /// Contains connection information for a SQL server queue
    /// </summary>
    public class ConnectionInformation: BaseConnectionInformation
    {
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="ConnectionInformation"/> class.
        /// </summary>
        /// <param name="queueConnection">Queue and connection information.</param>
        public ConnectionInformation(QueueConnection queueConnection) : base(queueConnection)
        {

        }
        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the server.
        /// </summary>
        /// <value>
        /// The server.
        /// </value>
        public override string Server => ConnectionString;

        /// <summary>
        /// Gets the container.
        /// </summary>
        /// <value>
        /// The container.
        /// </value>
        /// <remarks>
        /// The name of the container for the queue
        /// </remarks>
        public override string Container => QueueName;

        #endregion

        #region IClone
        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        public override IConnectionInformation Clone()
        {
            var data = new Dictionary<string, string>();
            foreach (var keyvalue in AdditionalConnectionSettings)
            {
                data.Add(keyvalue.Key, keyvalue.Value);
            }
            return new ConnectionInformation(new QueueConnection(QueueName, ConnectionString, data));
        }
        #endregion
    }
}
