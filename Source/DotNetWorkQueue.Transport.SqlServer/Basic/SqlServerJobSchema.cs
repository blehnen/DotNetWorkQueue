using System.Collections.Generic;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Transport.SqlServer.Schema;

namespace DotNetWorkQueue.Transport.SqlServer.Basic
{
    /// <summary>
    /// Creates a table that stores data about scheduled jobs
    /// </summary>
    public class SqlServerJobSchema: IJobSchema
    {
        private readonly TableNameHelper _tableNameHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerJobSchema"/> class.
        /// </summary>
        /// <param name="tableNameHelper">The table name helper.</param>
        public SqlServerJobSchema(TableNameHelper tableNameHelper)
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
            var main = new Table(GetOwner(), _tableNameHelper.JobTableName);
            main.Columns.Add(new Column("JobEventTime", ColumnTypes.Datetimeoffset, false, null));
            main.Columns.Add(new Column("JobScheduledTime", ColumnTypes.Datetimeoffset, false, null));
            main.Columns.Add(new Column("JobName", ColumnTypes.Varchar, 255, false, null));

            //add primary key constraint
            main.Constraints.Add(new Constraint("PK_" + _tableNameHelper.JobTableName, ConstraintType.PrimaryKey, "JobName"));
            main.PrimaryKey.Clustered = true;
            main.PrimaryKey.Unique = true;
            return main;
        }
        /// <summary>
        /// Gets the schema owner
        /// </summary>
        /// <remarks>This is always 'dbo'</remarks>
        /// <returns></returns>
        private string GetOwner()
        {
            return "dbo";
        }
    }
}
