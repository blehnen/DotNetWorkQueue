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
namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// A helper class that outputs the name for a given queue table, given the base name of the queue.
    /// </summary>
    public class TableNameHelper
    {
        private readonly IConnectionInformation _connectionInformation;
        private const string NameNotSet = "Error-Name-Not-Set";
        private const string JobsTableName = "DNWQJobs";

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="TableNameHelper" /> class.
        /// </summary>
        /// <param name="connectionInformation">The connection information.</param>
        public TableNameHelper(IConnectionInformation connectionInformation)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            _connectionInformation = connectionInformation;
        }
        #endregion

        #region Table Names

        /// <summary>
        /// Returns all table names.
        /// </summary>
        /// <value>
        /// The tables.
        /// </value>
        public List<string> Tables
        {
            get
            {
                var tables = new List<string>
                {
                    QueueName,
                    MetaDataName,
                    StatusName,
                    ConfigurationName,
                    ErrorTrackingName,
                    MetaDataErrorsName
                };
                return tables;
            }
        }

        /// <summary>
        /// Returns the name of the queue table. This table stores the message itself.
        /// </summary>
        /// <value>
        /// The name of the queue.
        /// </value>
        public string QueueName => _connectionInformation.QueueName;

        /// <summary>
        /// Returns the name of the queue meta data table. This table stores the data used to find records to process.
        /// </summary>
        /// <value>
        /// The name of the meta data.
        /// </value>
        public string MetaDataName => !string.IsNullOrEmpty(QueueName) ? string.Concat(QueueName, "MetaData") : NameNotSet;

        /// <summary>
        /// Gets the name of the status table.
        /// </summary>
        /// <value>
        /// The name of the status.
        /// </value>
        public string StatusName => !string.IsNullOrEmpty(QueueName) ? string.Concat(QueueName, "Status") : NameNotSet;

        /// <summary>
        /// Returns the name of the queue configuration table.
        /// </summary>
        /// <value>
        /// The name of the configuration.
        /// </value>
        public string ConfigurationName => !string.IsNullOrEmpty(QueueName) ? string.Concat(QueueName, "Configuration") : NameNotSet;

        /// <summary>
        /// Returns the name of the queue error tracking table.
        /// </summary>
        /// <value>
        /// The name of the error tracking.
        /// </value>
        public string ErrorTrackingName => !string.IsNullOrEmpty(QueueName) ? string.Concat(QueueName, "ErrorTracking") : NameNotSet;

        /// <summary>
        /// Returns the name of the queue table that stores records that have failed to process. This is a copy of the meta data table, along with a last exception.
        /// </summary>
        /// <value>
        /// The name of the meta data errors.
        /// </value>
        public string MetaDataErrorsName => !string.IsNullOrEmpty(QueueName) ? string.Concat(QueueName, "MetaDataErrors") : NameNotSet;

        /// <summary>
        /// Gets the name of the job table.
        /// </summary>
        /// <value>
        /// The name of the job table.
        /// </value>
        public string JobTableName => JobsTableName;


        #endregion
    }
}
