using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.RelationalDatabase.Basic.Query;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic
{
    public class GetColumnsFromTable: IGetColumnsFromTable
    {
        private readonly IConnectionInformation _connectionInformation;
        private readonly IQueryHandler<GetColumnNamesFromTableQuery, List<string>> _columnQuery;

        public GetColumnsFromTable(IConnectionInformation connectionInformation,
            IQueryHandler<GetColumnNamesFromTableQuery, List<string>> columnQuery)
        {
            Guard.NotNull(() => connectionInformation, connectionInformation);
            Guard.NotNull(() => columnQuery, columnQuery);

            _connectionInformation = connectionInformation;
            _columnQuery = columnQuery;
        }
        public IEnumerable<string> GetColumnsThatAreInBothTables(string table1, string table2)
        {
            var columns1 = _columnQuery.Handle(new GetColumnNamesFromTableQuery(_connectionInformation.ConnectionString, table1));
            var columns2 = _columnQuery.Handle(new GetColumnNamesFromTableQuery(_connectionInformation.ConnectionString, table2));
            return columns1.Intersect(columns2, StringComparer.InvariantCultureIgnoreCase);
        }
    }
}
