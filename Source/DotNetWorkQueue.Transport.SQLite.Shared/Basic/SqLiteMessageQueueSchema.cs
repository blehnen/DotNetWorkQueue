using System;
using System.Collections.Generic;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.SQLite.Shared.Schema;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Basic
{
    /// <summary>
    /// Defines the schema for a queue
    /// </summary>
    public class SqLiteMessageQueueSchema
    {
        private readonly TableNameHelper _tableNameHelper;
        private readonly Lazy<SqLiteMessageQueueTransportOptions> _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqLiteMessageQueueSchema"/> class.
        /// </summary>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="options">The options.</param>
        public SqLiteMessageQueueSchema(TableNameHelper tableNameHelper,
            ISqLiteMessageQueueTransportOptionsFactory options)
        {
            Guard.NotNull(() => tableNameHelper, tableNameHelper);
            Guard.NotNull(() => options, options);

            _tableNameHelper = tableNameHelper;
            _options = new Lazy<SqLiteMessageQueueTransportOptions>(options.Create);
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
            var mainPrimaryKey = new Column("QueueID", ColumnTypes.Integer, false, null) {Identity = new Identity()};

            main.Columns.Add(mainPrimaryKey);
            main.Columns.Add(new Column("Body", ColumnTypes.Blob, -1, false, null));
            main.Columns.Add(new Column("Headers", ColumnTypes.Blob, -1, false, null));

            return main;
        }
        /// <summary>
        /// Creates the configuration table schema.
        /// </summary>
        /// <returns></returns>
        private Table CreateConfigurationTable()
        {
            var table = new Table(_tableNameHelper.ConfigurationName);
            var mainPrimaryKey = new Column("ID", ColumnTypes.Integer, false, null) {Identity = new Identity()};
            table.Columns.Add(mainPrimaryKey);
            table.Columns.Add(new Column("Configuration", ColumnTypes.Blob, -1, false, null));

            return table;
        }

        private Table CreateStatusTable()
        {
            //--Meta Data Table --
            var status = new Table(_tableNameHelper.StatusName);
            var mainPrimaryKey = new Column("QueueID", ColumnTypes.Integer, false, null);
            status.Columns.Add(mainPrimaryKey);

            //add primary key constraint
            status.Constraints.Add(new Constraint("PK_" + _tableNameHelper.StatusName, ConstraintType.PrimaryKey, "QueueID"));
            status.PrimaryKey.Unique = true;

            status.Columns.Add(new Column("Status", ColumnTypes.Integer, false, null));
            status.Columns.Add(new Column("CorrelationID", ColumnTypes.Text, 38, false, null));

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
            var mainPrimaryKey = new Column("QueueID", ColumnTypes.Integer, false, null);
            meta.Columns.Add(mainPrimaryKey);

            //add primary key constraint
            meta.Constraints.Add(new Constraint("PK_" + _tableNameHelper.MetaDataName, ConstraintType.PrimaryKey, "QueueID"));
            meta.PrimaryKey.Unique = true;

            if (_options.Value.EnablePriority)
            {
                meta.Columns.Add(new Column("Priority", ColumnTypes.Integer, false, null));
            }

            meta.Columns.Add(new Column("QueuedDateTime", ColumnTypes.Integer, false, null));

            if (_options.Value.EnableStatus)
            {
                meta.Columns.Add(new Column("Status", ColumnTypes.Integer, false, null));
            }
            if (_options.Value.EnableDelayedProcessing)
            {
                meta.Columns.Add(new Column("QueueProcessTime", ColumnTypes.Integer, false, null));
            }

            meta.Columns.Add(new Column("CorrelationID", ColumnTypes.Text, 38, false, null));

            if (_options.Value.EnableHeartBeat)
            {
                meta.Columns.Add(new Column("HeartBeat", ColumnTypes.Integer, true, null));
            }

            if (_options.Value.EnableMessageExpiration || _options.Value.QueueType == QueueTypes.RpcReceive || _options.Value.QueueType == QueueTypes.RpcSend)
            {
                meta.Columns.Add(new Column("ExpirationTime", ColumnTypes.Integer, false, null));
            }

            if (_options.Value.EnableRoute)
            {
                meta.Columns.Add(new Column("Route", ColumnTypes.Text, 255, true, null));
            }

            switch (_options.Value.QueueType)
            {
                case QueueTypes.RpcReceive:
                    meta.Columns.Add(new Column("SourceQueueID", ColumnTypes.Integer, false, null));
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
                var cluster = new Constraint("IX_DeQueue", ConstraintType.Index, clusterIndex)
                {
                    Unique = true
                };
                meta.Constraints.Add(cluster);
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
            var errorTracking = new Table(_tableNameHelper.ErrorTrackingName);
            var errorTrackingPrimaryKey = new Column("ErrorTrackingID", ColumnTypes.Integer, false, null)
            {
                Identity = new Identity()
            };
            errorTracking.Columns.Add(errorTrackingPrimaryKey);

            errorTracking.Columns.Add(new Column("QueueID", ColumnTypes.Integer, false, null));
            errorTracking.Columns.Add(new Column("ExceptionType", ColumnTypes.Text, 500, false, null));
            errorTracking.Columns.Add(new Column("RetryCount", ColumnTypes.Integer, false, null));

            //add primary key constraint
            //errorTracking.Constraints.Add(new Constraint("PK_" + _tableNameHelper.ErrorTrackingName, ConstraintType.PrimaryKey, "ErrorTrackingID"));
            //errorTracking.PrimaryKey.Unique = true;

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
            var metaErrors = new Table(_tableNameHelper.MetaDataErrorsName);
            var primaryKey = new Column("ID", ColumnTypes.Integer, false, null) {Identity = new Identity()};
            metaErrors.Columns.Add(primaryKey);
            foreach (var c in meta.Columns.Items)
            {
                metaErrors.Columns.Add(c.Clone());
            }
            metaErrors.Columns.Add(new Column("LastException", ColumnTypes.Text, -1, true, null));
            metaErrors.Columns.Add(new Column("LastExceptionDate", ColumnTypes.Integer, true, null));

            //add primary key constraint
            //metaErrors.Constraints.Add(new Constraint("PK_" + _tableNameHelper.MetaDataErrorsName, ConstraintType.PrimaryKey, "ID"));
            //metaErrors.PrimaryKey.Unique = true;

            //NOTE no indexes are copied from the meta table
            foreach (var c in metaErrors.Constraints)
            {
                c.Table = metaErrors.Info;
            }

            return metaErrors;
        }
    }
}
