using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DotNetWorkQueue.Exceptions;

namespace DotNetWorkQueue.Transport.SQLite.Shared.Schema
{
    /// <summary>
    /// A collection of <seealso cref="Column"/>
    /// </summary>
    [SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable", Justification = "Not supported by children")]
    public class ColumnList: Dictionary<string, Column>
    {
        /// <summary>
        /// Adds the specified column.
        /// </summary>
        /// <param name="column">The column.</param>
        public void Add(Column column)
        {
            if (!ContainsKey(column.Name))
            {
                Add(column.Name, column);
            }
            else
            {
                throw new DotNetWorkQueueException($"Duplicate column name {column.Name}");
            }
        }

        /// <summary>
        /// Removes the specified column.
        /// </summary>
        /// <param name="column">The column.</param>
        public void Remove(Column column)
        {
            if (ContainsKey(column.Name))
            {
                Remove(column.Name);
            }
        }
    }
}
