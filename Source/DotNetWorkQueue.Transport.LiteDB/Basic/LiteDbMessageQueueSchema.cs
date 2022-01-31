// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
using DotNetWorkQueue.Transport.LiteDb.Schema;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.LiteDb.Basic
{
    /// <summary>
    /// Defines the schema for a queue
    /// </summary>
    public class LiteDbMessageQueueSchema
    {
        private readonly Lazy<LiteDbMessageQueueTransportOptions> _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="LiteDbMessageQueueSchema"/> class.
        /// </summary>
        /// <param name="options">The options.</param>
        public LiteDbMessageQueueSchema(
            ILiteDbMessageQueueTransportOptionsFactory options)
        {
            Guard.NotNull(() => options, options);

            _options = new Lazy<LiteDbMessageQueueTransportOptions>(options.Create);
        }

        /// <summary>
        /// Returns our schema as a list of the collections that use in LiteDb
        /// </summary>
        /// <returns></returns>
        public List<ITable> GetSchema()
        {
            var rc = new List<ITable>
            {
                CreateMainTable(),
                CreateMetaDataTable(),
                CreateErrorTrackingTable(),
                CreateErrorTable(),
                CreateConfigurationTable()
            };
            if (_options.Value.EnableStatusTable)
            {
                rc.Add(CreateStatusTable());
            }

            return rc;
        }
        /// <summary>
        /// Creates the main table schema
        /// </summary>
        /// <returns></returns>
        private Schema.QueueTable CreateMainTable()
        {
            return new Schema.QueueTable();
        }
        /// <summary>
        /// Creates the configuration table schema.
        /// </summary>
        /// <returns></returns>
        private Schema.ConfigurationTable CreateConfigurationTable()
        {
            return new Schema.ConfigurationTable();
        }

        private Schema.StatusTable CreateStatusTable()
        {
            return new StatusTable();
        }
        /// <summary>
        /// Creates the meta data table schema.
        /// </summary>
        /// <returns></returns>
        private Schema.MetaDataTable CreateMetaDataTable()
        {
            return new MetaDataTable();
        }
        /// <summary>
        /// Creates the error tracking table schema.
        /// </summary>
        /// <returns></returns>
        private Schema.ErrorTrackingTable CreateErrorTrackingTable()
        {
            return new ErrorTrackingTable();
        }
        /// <summary>
        /// Creates the error table schema. This is a copy of the meta table, but with an exception column added. 
        /// </summary>
        /// <returns></returns>
        private Schema.MetaDataErrorsTable CreateErrorTable()
        {
            return new MetaDataErrorsTable();
        }
    }
}
