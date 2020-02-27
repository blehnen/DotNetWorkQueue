using System.Collections.Generic;
using DotNetWorkQueue.Validation;

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
        /// Gets the status.
        /// </summary>
        /// <value>
        /// The status.
        /// </value>
        public string Status
        {
            get
            {
                BuildListIfNeeded();
                return _names["Status"];
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
        /// Gets the job event.
        /// </summary>
        /// <value>
        /// The job event.
        /// </value>
        public string JobEvent
        {
            get
            {
                BuildListIfNeeded();
                return _names["JobEvent"];
            }
        }

        /// <summary>
        /// Gets the job event.
        /// </summary>
        /// <value>
        /// The job event.
        /// </value>
        public string Route
        {
            get
            {
                BuildListIfNeeded();
                return _names["Route"];
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
        /// Gets the job names.
        /// </summary>
        /// <value>
        /// The job names.
        /// </value>
        public string JobNames
        {
            get
            {
                BuildListIfNeeded();
                return _names["JobNames"];
            }
        }

        /// <summary>
        /// Gets the job identifier names.
        /// </summary>
        /// <value>
        /// The job identifier names.
        /// </value>
        public string JobIdNames
        {
            get
            {
                BuildListIfNeeded();
                return _names["JobIDNames"];
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
        /// Tracks when a record was placed in an error status
        /// </summary>
        public string ErrorTime
        {
            get
            {
                BuildListIfNeeded();
                return _names["ErrorTime"];
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
        /// Returns the key name for a pending route
        /// </summary>
        /// <param name="route">The route.</param>
        /// <returns></returns>
        public string PendingRoute(string route)
        {
            BuildListIfNeeded();
            return string.Concat(_names["Pending"], "_}", route);
        }

        /// <summary>
        /// Gets all key names
        /// </summary>
        /// <value>
        /// The values.
        /// </value>
        /// <remarks>The notification name is a channel, not a key; it is not included in this property. Does not include route keys.</remarks>
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
                yield return JobNames;
                yield return JobIdNames;
                yield return Status;
                yield return ErrorTime;
                yield return Route;
            }
        } 

        /// <summary>
        /// Builds the queue names based on the root queue
        /// </summary>
        private void BuildListIfNeeded()
        {
            if (_names.Count == 16) return; //don't return unless all names are loaded
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
                _names.Add("ErrorTime", string.Concat(QueuePrefix, _connectionInformation.QueueName, "_}ErrorTime"));
                _names.Add("Expiration", string.Concat(QueuePrefix, _connectionInformation.QueueName, "_}Expiration"));
                _names.Add("Id", string.Concat(QueuePrefix, _connectionInformation.QueueName, "_}Id"));
                _names.Add("Headers", string.Concat(QueuePrefix, _connectionInformation.QueueName, "_}Headers"));
                _names.Add("JobNames", string.Concat(QueuePrefix, _connectionInformation.QueueName, "_}JobNames"));
                _names.Add("JobIDNames", string.Concat(QueuePrefix, _connectionInformation.QueueName, "_}JobIDNames"));
                _names.Add("Status", string.Concat(QueuePrefix, _connectionInformation.QueueName, "_}Status"));
                _names.Add("Route", string.Concat(QueuePrefix, _connectionInformation.QueueName, "_}Routes"));
                _names.Add("JobEvent", string.Concat(QueuePrefix, "}JobEvent"));  //NOTE - not part of a queue
            }
        }
    }
}
