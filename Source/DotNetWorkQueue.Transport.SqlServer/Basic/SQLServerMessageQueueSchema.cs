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
using System;
using System.Collections.Generic;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.SqlServer.Schema;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SqlServer.Basic
{
    /// <summary>
    /// Defines the schema for a queue
    /// </summary>
    public class SqlServerMessageQueueSchema
    {
        private readonly ITableNameHelper _tableNameHelper;
        private readonly Lazy<SqlServerMessageQueueTransportOptions> _options;
        private readonly ISqlSchema _schema;

        /// <summary>Initializes a new instance of the <see cref="SqlServerMessageQueueSchema"/> class.</summary>
        /// <param name="tableNameHelper">The table name helper. Note this is the base module</param>
        /// <param name="options">The options.</param>
        /// <param name="schema">The schema that the queue is using</param>
        public SqlServerMessageQueueSchema(TableNameHelper tableNameHelper,
            ISqlServerMessageQueueTransportOptionsFactory options, ISqlSchema schema)
        {
            Guard.NotNull(() => tableNameHelper, tableNameHelper);
            Guard.NotNull(() => options, options);

            _tableNameHelper = tableNameHelper;
            _options = new Lazy<SqlServerMessageQueueTransportOptions>(options.Create);
            _schema = schema;
        } 

        /// <summary>
        /// Returns our schema as a list of tables.
        /// </summary>
        /// <returns></returns>
        public List<ITable> GetSchema()
        {
            var meta = CreateMetaDataTable();
            var rc = new List<ITable>
            {
                CreateMainTable(),
                meta,
                CreateErrorTrackingTable(),
                CreateErrorTable(meta),
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
        private Table CreateMainTable()
        {
            //--main data table--
            var main = new Table(GetOwner(), _tableNameHelper.QueueName);
            var mainPrimaryKey = new Column("QueueID", ColumnTypes.Bigint, false, null) {Identity = new Identity(1, 1)};

            main.Columns.Add(mainPrimaryKey);
            main.Columns.Add(new Column("Body", ColumnTypes.Varbinary, -1, false, null));
            main.Columns.Add(new Column("Headers", ColumnTypes.Varbinary, -1, false, null));

            //add primary key constraint
            main.Constraints.Add(new Constraint("PK_" + _tableNameHelper.QueueName, ConstraintType.PrimaryKey, "QueueID"));
            main.PrimaryKey.Clustered = true;
            main.PrimaryKey.Unique = true;
            return main;
        }
        /// <summary>
        /// Creates the configuration table schema.
        /// </summary>
        /// <returns></returns>
        private Table CreateConfigurationTable()
        {
            var table = new Table(GetOwner(), _tableNameHelper.ConfigurationName);
            var mainPrimaryKey = new Column("ID", ColumnTypes.Int, false, null) {Identity = new Identity(1, 1)};
            table.Columns.Add(mainPrimaryKey);
            table.Columns.Add(new Column("Configuration", ColumnTypes.Varbinary, -1, false, null));

            table.Constraints.Add(new Constraint("PK_" + _tableNameHelper.ConfigurationName, ConstraintType.PrimaryKey, "ID"));
            table.PrimaryKey.Clustered = true;
            table.PrimaryKey.Unique = true;
            return table;
        }

        private Table CreateStatusTable()
        {
            //--Status Table --
            var status = new Table(GetOwner(), _tableNameHelper.StatusName);
            var mainPrimaryKey = new Column("QueueID", ColumnTypes.Bigint, false, null);
            status.Columns.Add(mainPrimaryKey);

            //add primary key constraint
            status.Constraints.Add(new Constraint("PK_" + _tableNameHelper.StatusName, ConstraintType.PrimaryKey, "QueueID"));
            status.PrimaryKey.Unique = true;
            status.PrimaryKey.Clustered = true;

            status.Columns.Add(new Column("Status", ColumnTypes.Int, false, null));
            status.Columns.Add(new Column("CorrelationID", ColumnTypes.Uniqueidentifier, false, null));

            if (!_options.Value.AdditionalColumnsOnMetaData)
            {
                //add extra user columns
                foreach (var c in _options.Value.AdditionalColumns.Values)
                {
                    status.Columns.Add(c);
                }

                //add extra user constrains
                foreach (var c in _options.Value.AdditionalConstraints.Values)
                {
                    status.Constraints.Add(c);
                }
            }

            //set the table reference
            foreach (var c in status.Constraints)
            {
                c.Table = status.Info;
            }

            return status;
        }
        /// <summary>
        /// Creates the meta data table schema.
        /// </summary>
        /// <returns></returns>
        private Table CreateMetaDataTable()
        {
            //--Meta Data Table --
            var meta = new Table(GetOwner(), _tableNameHelper.MetaDataName);
            var mainPrimaryKey = new Column("QueueID", ColumnTypes.Bigint, false, null);
            meta.Columns.Add(mainPrimaryKey);

            //add primary key constraint
            meta.Constraints.Add(new Constraint("PK_" + _tableNameHelper.MetaDataName, ConstraintType.PrimaryKey, "QueueID"));
            meta.PrimaryKey.Unique = true;

            if (_options.Value.EnablePriority)
            {
                meta.Columns.Add(new Column("Priority", ColumnTypes.Tinyint, false, null));
            }

            meta.Columns.Add(new Column("QueuedDateTime", ColumnTypes.Datetime, false, null));

            if (_options.Value.EnableStatus)
            {
                meta.Columns.Add(new Column("Status", ColumnTypes.Int, false, null));
            }
            if (_options.Value.EnableDelayedProcessing)
            {
                meta.Columns.Add(new Column("QueueProcessTime", ColumnTypes.Datetime, false, null));
            }

            meta.Columns.Add(new Column("CorrelationID", ColumnTypes.Uniqueidentifier, false, null));

            if (_options.Value.EnableHeartBeat)
            {
                meta.Columns.Add(new Column("HeartBeat", ColumnTypes.Datetime, true, null));
            }

            if (_options.Value.EnableMessageExpiration)
            {
                meta.Columns.Add(new Column("ExpirationTime", ColumnTypes.Datetime, false, null));
            }

            if (_options.Value.EnableRoute)
            {
                meta.Columns.Add(new Column("Route", ColumnTypes.Varchar, 255, true, null));
            }


            if (_options.Value.AdditionalColumnsOnMetaData)
            {
                //add extra user columns
                foreach (var c in _options.Value.AdditionalColumns.Values)
                {
                    meta.Columns.Add(c);
                }

                //add extra user constrains
                foreach (var c in _options.Value.AdditionalConstraints.Values)
                {
                    meta.Constraints.Add(c);
                }
            }

            var clusterIndex = new List<string>();
            if (_options.Value.EnableStatus)
            {
                clusterIndex.Add("Status");
            }
            if (_options.Value.EnablePriority)
            {
                clusterIndex.Add("Priority");
            }
            if (_options.Value.EnableDelayedProcessing)
            {
                clusterIndex.Add("QueueProcessTime");
            }
            if (_options.Value.EnableRoute)
            {
                clusterIndex.Add("Route");
            }
            //add index on expiration time if needed
            if (_options.Value.EnableMessageExpiration)
            {
                clusterIndex.Add("ExpirationTime");
            }

            if (clusterIndex.Count > 0)
            {
                clusterIndex.Add("QueueID");
                var cluster = new Constraint("IX_DeQueue", ConstraintType.Index, clusterIndex)
                {
                    Clustered = true,
                    Unique = true
                };
                meta.Constraints.Add(cluster);
            }
            else //this is a FIFO queue, just cluster on the primary key
            {
                meta.PrimaryKey.Clustered = true;
            }

            //add index on heartbeat column if enabled
            if (_options.Value.EnableHeartBeat)
            {
                meta.Constraints.Add(new Constraint("IX_HeartBeat", ConstraintType.Index, "HeartBeat"));
            }

            //set the table reference
            foreach (var c in meta.Constraints)
            {
                c.Table = meta.Info;
            }

            return meta;
        }
        /// <summary>
        /// Creates the error tracking table schema.
        /// </summary>
        /// <returns></returns>
        private Table CreateErrorTrackingTable()
        {
            //--Error tracking table--
            var errorTracking = new Table(GetOwner(), _tableNameHelper.ErrorTrackingName);
            var errorTrackingPrimaryKey = new Column("ErrorTrackingID", ColumnTypes.Bigint, false, null)
            {
                Identity = new Identity(1, 1)
            };
            errorTracking.Columns.Add(errorTrackingPrimaryKey);

            errorTracking.Columns.Add(new Column("QueueID", ColumnTypes.Bigint, false, null));
            errorTracking.Columns.Add(new Column("ExceptionType", ColumnTypes.Varchar, 500, false, null));
            errorTracking.Columns.Add(new Column("RetryCount", ColumnTypes.Int, false, null));

            //add primary key constraint
            errorTracking.Constraints.Add(new Constraint("PK_" + _tableNameHelper.ErrorTrackingName, ConstraintType.PrimaryKey, "ErrorTrackingID"));
            errorTracking.PrimaryKey.Clustered = true;
            errorTracking.PrimaryKey.Unique = true;

            errorTracking.Constraints.Add(new Constraint("IX_QueueID", ConstraintType.Index, "QueueID"));

            foreach (var c in errorTracking.Constraints)
            {
                c.Table = errorTracking.Info;
            }

            return errorTracking;
        }
        /// <summary>
        /// Creates the error table schema. This is a copy of the meta table, but with an exception column added. 
        /// </summary>
        /// <param name="meta">The meta.</param>
        /// <returns></returns>
        private Table CreateErrorTable(Table meta)
        {
            var metaErrors = new Table(GetOwner(), _tableNameHelper.MetaDataErrorsName);
            var primaryKey = new Column("ID", ColumnTypes.Bigint, false, null) {Identity = new Identity(1, 1)};
            metaErrors.Columns.Add(primaryKey);
            foreach (var c in meta.Columns.Items)
            {
                metaErrors.Columns.Add(c.Clone());
            }
            metaErrors.Columns.Add(new Column("LastException", ColumnTypes.Varchar, -1, true, null));
            metaErrors.Columns.Add(new Column("LastExceptionDate", ColumnTypes.Datetime, true, null));

            //add primary key constraint
            metaErrors.Constraints.Add(new Constraint("PK_" + _tableNameHelper.MetaDataErrorsName, ConstraintType.PrimaryKey, "ID"));
            metaErrors.PrimaryKey.Clustered = true;
            metaErrors.PrimaryKey.Unique = true;

            //NOTE no indexes are copied from the meta table
            foreach (var c in metaErrors.Constraints)
            {
                c.Table = metaErrors.Info;
            }

            return metaErrors;
        }
        /// <summary>
        /// Gets the schema owner
        /// </summary>
        /// <returns></returns>
        private string GetOwner()
        {
            return _schema.Schema;
        }
    }
}
