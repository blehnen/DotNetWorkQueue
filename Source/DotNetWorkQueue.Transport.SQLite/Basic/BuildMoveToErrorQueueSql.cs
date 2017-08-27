using System.Linq;
using System.Text;
using DotNetWorkQueue.Transport.RelationalDatabase;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.SQLite.Basic
{
    /// <summary>
    /// 
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.Transport.RelationalDatabase.IBuildMoveToErrorQueueSql" />
    public class BuildMoveToErrorQueueSql : IBuildMoveToErrorQueueSql
    {
        private readonly TableNameHelper _tableNameHelper;
        private readonly IGetColumnsFromTable _getColumns;

        private readonly object _buildSqlLocker = new object();
        private string _moveRecordSql;

        /// <summary>
        /// Initializes a new instance of the <see cref="BuildMoveToErrorQueueSql" /> class.
        /// </summary>
        /// <param name="tableNameHelper">The table name helper.</param>
        /// <param name="getColumns">The column query.</param>
        public BuildMoveToErrorQueueSql(TableNameHelper tableNameHelper,
            IGetColumnsFromTable getColumns)
        {
            Guard.NotNull(() => tableNameHelper, tableNameHelper);
            Guard.NotNull(() => getColumns, getColumns);

            _tableNameHelper = tableNameHelper;
            _getColumns = getColumns;
        }

        /// <summary>
        /// Creates the sql statement to move a record to the error queue
        /// </summary>
        /// <returns></returns>
        public string Create()
        {
            GenerateSqlForMove();
            return _moveRecordSql;
        }

        /// <summary>
        /// Generates the SQL for moving a record to the error queue.
        /// </summary>
        private void GenerateSqlForMove()
        {
            if (!string.IsNullOrEmpty(_moveRecordSql)) return;
            lock (_buildSqlLocker)
            {
                if (string.IsNullOrEmpty(_moveRecordSql))
                {
                    _moveRecordSql = BuildMoveRecordToErrorQueueSql();
                }
            }
        }

        /// <summary>
        /// Builds the move record to error queue SQL.
        /// </summary>
        /// <returns></returns>
        private string BuildMoveRecordToErrorQueueSql()
        {
            var columnsToCopy = _getColumns
                .GetColumnsThatAreInBothTables(_tableNameHelper.MetaDataErrorsName, _tableNameHelper.MetaDataName)
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("Insert into " + _tableNameHelper.MetaDataErrorsName);
            sb.Append(" ( ");
            var i = 0;
            foreach (var column in columnsToCopy)
            {
                sb.Append(column);
                if (i < columnsToCopy.Count - 1)
                {
                    sb.Append(",");
                }
                i++;
            }
            sb.Append(", LastException, LastExceptionDate)");

            sb.Append(" select ");
            i = 0;
            foreach (var column in columnsToCopy)
            {
                sb.Append(column);
                if (i < columnsToCopy.Count - 1)
                {
                    sb.Append(",");
                }
                i++;
            }
            sb.Append(", @LastException, @CurrentDateTime from " + _tableNameHelper.MetaDataName);
            sb.Append(" where queueid = @queueid");
            return sb.ToString();
        }
    }
}
