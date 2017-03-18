// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
using DotNetWorkQueue.Transport.SqlServer.Schema;
namespace DotNetWorkQueue.Transport.SqlServer.Basic
{
    /// <summary>
    /// Creates a table that stores data about scheduled jobs
    /// </summary>
    public class SqlServerJobSchema
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
        public List<Table> GetSchema()
        {
            var rc = new List<Table>
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
            main.Constraints.Add(new Constraint("PK_" + _tableNameHelper.JobTableName, ContraintType.PrimaryKey, "JobName"));
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
