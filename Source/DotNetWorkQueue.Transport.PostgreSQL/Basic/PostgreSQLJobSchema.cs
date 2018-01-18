using System.Collections.Generic;
using DotNetWorkQueue.Transport.PostgreSQL.Schema;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;

namespace DotNetWorkQueue.Transport.PostgreSQL.Basic
{
    /// <summary>
    /// Creates a table that stores data about scheduled jobs
    /// </summary>
    public class PostgreSqlJobSchema: IJobSchema
    {
        private readonly TableNameHelper _tableNameHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgreSqlJobSchema"/> class.
        /// </summary>
        /// <param name="tableNameHelper">The table name helper.</param>
        public PostgreSqlJobSchema(TableNameHelper tableNameHelper)
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
            main.Columns.Add(new Column("JobEventTime", ColumnTypes.Bigint, false));
            main.Columns.Add(new Column("JobScheduledTime", ColumnTypes.Bigint, false));
            main.Columns.Add(new Column("JobName", ColumnTypes.Varchar, 255, false));

            //add primary key constraint
            main.Constraints.Add(new Constraint("PK_" + _tableNameHelper.JobTableName, ConstraintType.PrimaryKey, "JobName"));
            main.PrimaryKey.Unique = true;
            return main;
        }
    }
}
