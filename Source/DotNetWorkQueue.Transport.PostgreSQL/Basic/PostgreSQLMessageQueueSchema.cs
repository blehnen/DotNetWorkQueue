using System;
using System.Collections.Generic;
using DotNetWorkQueue.Transport.PostgreSQL.Schema;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
{
    /// <summary>
    /// Defines the schema for a queue
    /// </summary>
    public class PostgreSqlMessageQueueSchema
    {
        private readonly TableNameHelper _tableNameHelper;
        private readonly Lazy<PostgreSqlMessageQueueTransportOptions> _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlMessageQueueSchema"/> class.
        /// </summary>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="options">The options.</param>
        public PostgreSqlMessageQueueSchema(TableNameHelper tableNameHelper,
            IPostgreSqlMessageQueueTransportOptionsFactory options)
        {
            Guard.NotNull(() => tableNameHelper, tableNameHelper);
            Guard.NotNull(() => options, options);

            _tableNameHelper = tableNameHelper;
            _options = new Lazy<PostgreSqlMessageQueueTransportOptions>(options.Create);
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
            var main = new Table(_tableNameHelper.QueueName);
            var mainPrimaryKey = new Column("QueueID", ColumnTypes.Bigint, false) {Identity = true};

            main.Columns.Add(mainPrimaryKey);
            main.Columns.Add(new Column("Body", ColumnTypes.Bytea, -1, false));
            main.Columns.Add(new Column("Headers", ColumnTypes.Bytea, -1, false));

            //add primary key constraint
            main.Constraints.Add(new Constraint("PK_" + _tableNameHelper.QueueName, ConstraintType.PrimaryKey, "QueueID"));
            main.PrimaryKey.Unique = true;
            return main;
        }
        /// <summary>
        /// Creates the configuration table schema.
        /// </summary>
        /// <returns></returns>
        private Table CreateConfigurationTable()
        {
            var table = new Table(_tableNameHelper.ConfigurationName);
            var mainPrimaryKey = new Column("ID", ColumnTypes.Integer, false) {Identity = true};
            table.Columns.Add(mainPrimaryKey);
            table.Columns.Add(new Column("Configuration", ColumnTypes.Bytea, -1, false));

            table.Constraints.Add(new Constraint("PK_" + _tableNameHelper.ConfigurationName, ConstraintType.PrimaryKey, "ID"));
            table.PrimaryKey.Unique = true;
            return table;
        }

        private Table CreateStatusTable()
        {
            //--Meta Data Table --
            var status = new Table(_tableNameHelper.StatusName);
            var mainPrimaryKey = new Column("QueueID", ColumnTypes.Bigint, false);
            status.Columns.Add(mainPrimaryKey);

            //add primary key constraint
            status.Constraints.Add(new Constraint("PK_" + _tableNameHelper.StatusName, ConstraintType.PrimaryKey, "QueueID"));
            status.PrimaryKey.Unique = true;

            status.Columns.Add(new Column("Status", ColumnTypes.Integer, false));
            status.Columns.Add(new Column("CorrelationID", ColumnTypes.Uuid, false));

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
            var meta = new Table(_tableNameHelper.MetaDataName);
            var mainPrimaryKey = new Column("QueueID", ColumnTypes.Bigint, false);
            meta.Columns.Add(mainPrimaryKey);

            //add primary key constraint
            meta.Constraints.Add(new Constraint("PK_" + _tableNameHelper.MetaDataName, ConstraintType.PrimaryKey, "QueueID"));
            meta.PrimaryKey.Unique = true;

            if (_options.Value.EnablePriority)
            {
                meta.Columns.Add(new Column("Priority", ColumnTypes.Integer, false));
            }

            meta.Columns.Add(new Column("QueuedDateTime", ColumnTypes.Timestamp, false));

            if (_options.Value.EnableStatus)
            {
                meta.Columns.Add(new Column("Status", ColumnTypes.Integer, false));
            }
            if (_options.Value.EnableDelayedProcessing)
            {
                meta.Columns.Add(new Column("QueueProcessTime", ColumnTypes.Bigint, false));
            }

            meta.Columns.Add(new Column("CorrelationID", ColumnTypes.Uuid, false));

            if (_options.Value.EnableHeartBeat)
            {
                meta.Columns.Add(new Column("HeartBeat", ColumnTypes.Bigint, true));
            }

            if (_options.Value.EnableRoute)
            {
                meta.Columns.Add(new Column("Route", ColumnTypes.Text, 255, true));
            }

            if (_options.Value.EnableMessageExpiration || _options.Value.QueueType == QueueTypes.RpcReceive || _options.Value.QueueType == QueueTypes.RpcSend)
            {
                meta.Columns.Add(new Column("ExpirationTime", ColumnTypes.Bigint, false));
            }

            switch (_options.Value.QueueType)
            {
                case QueueTypes.RpcReceive:
                    meta.Columns.Add(new Column("SourceQueueID", ColumnTypes.Bigint, false));
                    break;
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
            if (_options.Value.EnableMessageExpiration || _options.Value.QueueType == QueueTypes.RpcReceive || _options.Value.QueueType == QueueTypes.RpcSend)
            {
                clusterIndex.Add("ExpirationTime");
            }

            switch (_options.Value.QueueType)
            {
                case QueueTypes.RpcReceive:
                    clusterIndex.Add("SourceQueueID");
                    break;
            }

            if (clusterIndex.Count > 0)
            {
                clusterIndex.Add("QueueID");
            }

            if (clusterIndex.Count > 0)
            {
                var cluster = new Constraint($"IX_DeQueue{_tableNameHelper.MetaDataName}", ConstraintType.Index, clusterIndex)
                {
                    Unique = true
                };
                meta.Constraints.Add(cluster);
            }

            //add index on heartbeat column if enabled
            if (_options.Value.EnableHeartBeat)
            {
                meta.Constraints.Add(new Constraint($"IX_HeartBeat{_tableNameHelper.MetaDataName}", ConstraintType.Index, "HeartBeat"));
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
            var errorTracking = new Table(_tableNameHelper.ErrorTrackingName);
            var errorTrackingPrimaryKey = new Column("ErrorTrackingID", ColumnTypes.Bigint, false)
            {
                Identity = true
            };
            errorTracking.Columns.Add(errorTrackingPrimaryKey);

            errorTracking.Columns.Add(new Column("QueueID", ColumnTypes.Bigint, false));
            errorTracking.Columns.Add(new Column("ExceptionType", ColumnTypes.Varchar, 500, false));
            errorTracking.Columns.Add(new Column("RetryCount", ColumnTypes.Integer, false));

            //add primary key constraint
            errorTracking.Constraints.Add(new Constraint("PK_" + _tableNameHelper.ErrorTrackingName, ConstraintType.PrimaryKey, "ErrorTrackingID"));
            errorTracking.PrimaryKey.Unique = true;

            errorTracking.Constraints.Add(new Constraint($"IX_QueueID{_tableNameHelper.ErrorTrackingName}", ConstraintType.Index, "QueueID"));

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
            var metaErrors = new Table(_tableNameHelper.MetaDataErrorsName);
            var primaryKey = new Column("ID", ColumnTypes.Bigint, false) {Identity = true};
            metaErrors.Columns.Add(primaryKey);
            foreach (var c in meta.Columns.Items)
            {
                metaErrors.Columns.Add(c.Clone());
            }
            metaErrors.Columns.Add(new Column("LastException", ColumnTypes.Text, -1, true));
            metaErrors.Columns.Add(new Column("LastExceptionDate", ColumnTypes.Timestamp, true));

            //add primary key constraint
            metaErrors.Constraints.Add(new Constraint("PK_" + _tableNameHelper.MetaDataErrorsName, ConstraintType.PrimaryKey, "ID"));
            metaErrors.PrimaryKey.Unique = true;

            //NOTE no indexes are copied from the meta table
            foreach (var c in metaErrors.Constraints)
            {
                c.Table = metaErrors.Info;
            }

            return metaErrors;
        }
    }
}
