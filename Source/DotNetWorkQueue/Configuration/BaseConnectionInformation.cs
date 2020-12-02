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

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace DotNetWorkQueue.Configuration
{
    /// <inheritdoc />
    public class BaseConnectionInformation : IConnectionInformation
    {
        private readonly string _connectionString;
        private readonly string _queueName;
        private readonly int _hashCode;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseConnectionInformation"/> class.
        /// </summary>
        /// <param name="queueName">Name of the queue.</param>
        /// <param name="connectionString">The connection string.</param>
        public BaseConnectionInformation(QueueConnection queueConnection)
        {
            _queueName = queueConnection.Queue;
            _connectionString = queueConnection.Connection;
            AdditionalConnectionSettings = queueConnection.AdditionalConnectionSettings ?? new Dictionary<string, string>();
            _hashCode = CalculateHashCode();
        }

        #region Public Properties
        /// <summary>
        /// Gets or sets the connection string.
        /// </summary>
        /// <value>
        /// The connection string.
        /// </value>
        public virtual string ConnectionString => _connectionString;

        /// <summary>
        /// Gets or sets the name of the queue.
        /// </summary>
        /// <value>
        /// The name of the queue.
        /// </value>
        public virtual string QueueName => _queueName;

        ///<inheritdoc/>
        public virtual IReadOnlyDictionary<string, string> AdditionalConnectionSettings { get; }

        /// <summary>
        /// Gets the server.
        /// </summary>
        /// <value>
        /// The server.
        /// </value>
        /// <remarks>The server display name</remarks>
        public virtual string Server => "Base connection object cannot determine server";

        /// <summary>
        /// Gets the container.
        /// </summary>
        /// <value>
        /// The container.
        /// </value>
        /// <remarks>The name of the container for the queue</remarks>
        public virtual string Container => "Base connection object cannot determine container";
        #endregion

        #region IClone
        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>
        /// <returns>
        /// A new object that is a copy of this instance.
        /// </returns>
        public virtual IConnectionInformation Clone()
        {
            var data = new Dictionary<string, string>();
            foreach (var keyvalue in AdditionalConnectionSettings)
            {
                data.Add(keyvalue.Key, keyvalue.Value);
            }
            return new BaseConnectionInformation(new QueueConnection(QueueName, ConnectionString, data));
        }
        #endregion

        /// <summary>
        /// Returns a <see cref="string" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Join("|", _connectionString, _queueName);
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            // Check for null values and compare run-time types.
            if (obj == null || GetType() != obj.GetType())
                return false;

            var connection = (BaseConnectionInformation)obj;
            var result = (connection.AdditionalConnectionSettings == AdditionalConnectionSettings) || 
                         (connection.AdditionalConnectionSettings.Count == AdditionalConnectionSettings.Count && !connection.AdditionalConnectionSettings.Except(AdditionalConnectionSettings).Any());
            return result && _connectionString == connection.ConnectionString && _queueName == connection.QueueName;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return _hashCode;
        }

        /// <summary>
        /// Calculates the hash code.
        /// </summary>
        /// <returns></returns>
        protected int CalculateHashCode()
        {
            var hashCode = string.Concat(_connectionString, _queueName).GetHashCode();
            foreach (var keyvalue in AdditionalConnectionSettings)
            {
                hashCode = (hashCode * 397) ^ keyvalue.Key.GetHashCode();
                hashCode = (hashCode * 397) ^ keyvalue.Value.GetHashCode();
            }
            return hashCode;
        }
    }
}
