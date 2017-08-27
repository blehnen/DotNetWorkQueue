using System.Collections.Generic;
namespace DotNetWorkQueue.Transport.RelationalDatabase
{
    public interface IGetColumnsFromTable
    {
        /// <summary>
        /// Gets the columns that are in both tables.
        /// </summary>
        /// <param name="table1">table 1.</param>
        /// <param name="table2">table 2.</param>
        /// <returns></returns>
        IEnumerable<string> GetColumnsThatAreInBothTables(string table1, string table2);
    }
}
