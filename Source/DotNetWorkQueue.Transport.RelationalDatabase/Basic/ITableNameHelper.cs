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

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    /// <summary>
    /// A helper class that outputs the name for a given queue table, given the base name of the queue.
    /// </summary>
    public interface ITableNameHelper
    {
        /// <summary>
        /// Returns all table names.
        /// </summary>
        /// <value>
        /// The tables.
        /// </value>
        List<string> Tables { get; }

        /// <summary>
        /// Returns the name of the queue table. This table stores the message itself.
        /// </summary>
        /// <value>
        /// The name of the queue.
        /// </value>
        string QueueName { get; }

        /// <summary>
        /// Returns the name of the queue meta data table. This table stores the data used to find records to process.
        /// </summary>
        /// <value>
        /// The name of the meta data.
        /// </value>
        string MetaDataName { get; }

        /// <summary>
        /// Gets the name of the status table.
        /// </summary>
        /// <value>
        /// The name of the status.
        /// </value>
        string StatusName { get; }

        /// <summary>
        /// Returns the name of the queue configuration table.
        /// </summary>
        /// <value>
        /// The name of the configuration.
        /// </value>
        string ConfigurationName { get; }

        /// <summary>
        /// Returns the name of the queue error tracking table.
        /// </summary>
        /// <value>
        /// The name of the error tracking.
        /// </value>
        string ErrorTrackingName { get; }

        /// <summary>
        /// Returns the name of the queue table that stores records that have failed to process. This is a copy of the meta data table, along with a last exception.
        /// </summary>
        /// <value>
        /// The name of the meta data errors.
        /// </value>
        string MetaDataErrorsName { get; }

        /// <summary>
        /// Gets the name of the job table.
        /// </summary>
        /// <value>
        /// The name of the job table.
        /// </value>
        string JobTableName { get; }
    }
}