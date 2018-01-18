using System.Collections.Generic;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.SQLite.Shared.Schema;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Basic
{
    /// <summary>
    /// Creates a table that stores data about scheduled jobs
    /// </summary>
    public class SqliteJobSchema: IJobSchema
    {
        private readonly TableNameHelper _tableNameHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteJobSchema"/> class.
        /// </summary>
        /// <param name="tableNameHelper">The table name helper.</param>
        public SqliteJobSchema(TableNameHelper tableNameHelper)
        {
            _tableNameHelper = tableNameHelper;
        }
        /// <summary>
        /// Returns our schema as a list of tables.
        /// </summary>
        /// <returns></returns>
        public List<ITable> GetSchema()
        {
            var rc = new List<ITable>
            {
                CreateMainTable()
            };
            return rc;
        }

        /// <summary>
        /// Creates the main table schema
        /// </summary>
        /// <returns></returns>
        private Table CreateMainTable()
        {
            //--main table--
            var main = new Table(_tableNameHelper.JobTableName);
            main.Columns.Add(new Column("JobEventTime", ColumnTypes.Text, 35, false, null));
            main.Columns.Add(new Column("JobScheduledTime", ColumnTypes.Text, 35, false, null));
            main.Columns.Add(new Column("JobName", ColumnTypes.Text, 255, false, null));

            //add primary key constraint
            main.Constraints.Add(new Constraint("PK_" + _tableNameHelper.JobTableName, ConstraintType.PrimaryKey, "JobName"));
            main.PrimaryKey.Unique = true;
            return main;
        }
    }
}
