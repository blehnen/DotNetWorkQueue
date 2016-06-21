// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
namespace DotNetWorkQueue.Transport.Redis.Basic
{
    /// <summary>
    /// Contains the redis hash/key/etc names for a given queue name
    /// </summary>
    public class RedisNames
    {
        private readonly IConnectionInformation _connectionInformation;
        private readonly Dictionary<string, string> _names;
        private const string QueuePrefix = "{YADNQ_";
        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="RedisNames" /> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        public RedisNames(IConnectionInformation connectionInformation)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            _connectionInformation = connectionInformation;
            _names = new Dictionary<string, string>();
        }
        #endregion

        /// <summary>
        /// Gets the notification queue name
        /// </summary>
        /// <value>
        /// The notification queue name
        /// </value>
        public string Notification
        {
            get 
            { 
                BuildListIfNeeded();
                return _names["Notification"];
            }         
        }

        /// <summary>
        /// Gets the pending queue name
        /// </summary>
        /// <value>
        /// The pending queue name
        /// </value>
        public string Pending
        {
            get
            {
                BuildListIfNeeded();
                return _names["Pending"];
            }
        }

        /// <summary>
        /// Gets the working queue name
        /// </summary>
        /// <value>
        /// The working queue name
        /// </value>
        public string Working
        {
            get
            {
                BuildListIfNeeded();
                return _names["Working"];
            }
        }

        /// <summary>
        /// Gets the values queue name
        /// </summary>
        /// <value>
        /// The values queue name
        /// </value>
        public string Values
        {
            get
            {
                BuildListIfNeeded();
                return _names["Values"];
            }
        }

        /// <summary>
        /// Gets the headers queue name
        /// </summary>
        /// <value>
        /// The headers queue name
        /// </value>
        public string Headers
        {
            get
            {
                BuildListIfNeeded();
                return _names["Headers"];
            }
        }

        /// <summary>
        /// Gets the location of the next ID storage
        /// </summary>
        /// <value>
        /// Gets the location of the next ID storage
        /// </value>
        public string Id
        {
            get
            {
                BuildListIfNeeded();
                return _names["Id"];
            }
        }

        /// <summary>
        /// Gets the delayed queue name
        /// </summary>
        /// <value>
        /// The delayed queue name
        /// </value>
        public string Delayed
        {
            get
            {
                BuildListIfNeeded();
                return _names["Delayed"];
            }
        }

        /// <summary>
        /// Gets the meta data queue name
        /// </summary>
        /// <value>
        /// The meta data queue name
        /// </value>
        public string MetaData
        {
            get
            {
                BuildListIfNeeded();
                return _names["MetaData"];
            }
        }

        /// <summary>
        /// Gets the error queue name
        /// </summary>
        /// <value>
        /// The error queue name
        /// </value>
        public string Error
        {
            get
            {
                BuildListIfNeeded();
                return _names["Error"];
            }
        }

        /// <summary>
        /// Gets the expiration queue name
        /// </summary>
        /// <value>
        /// The expiration queue name
        /// </value>
        public string Expiration
        {
            get
            {
                BuildListIfNeeded();
                return _names["Expiration"];
            }
        }

        /// <summary>
        /// Gets all key names
        /// </summary>
        /// <value>
        /// The values.
        /// </value>
        /// <remarks>The notification name is a channel, not a key; it is not included in this property</remarks>
        public IEnumerable<string> KeyNames
        {
            get
            {
                yield return Delayed;
                yield return Expiration;
                yield return Id;
                yield return MetaData;
                yield return Pending;
                yield return Values;
                yield return Working;
                yield return Error;
                yield return Headers;
            }
        } 

        /// <summary>
        /// Builds the queue names based on the root queue
        /// </summary>
        private void BuildListIfNeeded()
        {
            if (_names.Count == 10) return; //don't return unless all names are loaded
            lock (_names)
            {
                if (_names.Count != 0) return;
                _names.Add("Notification", string.Concat(QueuePrefix, _connectionInformation.QueueName, "_}Notifications"));
                _names.Add("Pending", string.Concat(QueuePrefix, _connectionInformation.QueueName, "_}Pending"));
                _names.Add("Working", string.Concat(QueuePrefix, _connectionInformation.QueueName, "_}Working"));
                _names.Add("Values", string.Concat(QueuePrefix, _connectionInformation.QueueName, "_}Values"));
                _names.Add("Delayed", string.Concat(QueuePrefix, _connectionInformation.QueueName, "_}Delayed"));
                _names.Add("MetaData", string.Concat(QueuePrefix, _connectionInformation.QueueName, "_}MetaData"));
                _names.Add("Error", string.Concat(QueuePrefix, _connectionInformation.QueueName, "_}Error"));
                _names.Add("Expiration", string.Concat(QueuePrefix, _connectionInformation.QueueName, "_}Expiration"));
                _names.Add("Id", string.Concat(QueuePrefix, _connectionInformation.QueueName, "_}Id"));
                _names.Add("Headers", string.Concat(QueuePrefix, _connectionInformation.QueueName, "_}Headers"));
            }
        }
    }
}
