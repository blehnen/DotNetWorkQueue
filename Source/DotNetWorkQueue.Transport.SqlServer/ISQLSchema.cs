using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetWorkQueue.Transport.SqlServer
{
    /// <summary>
    /// Defines the schema to use for all operations
    /// </summary>
    public interface ISqlSchema
    {
        /// <summary>
        /// Gets the schema.
        /// </summary>
        /// <remarks>Defaults to dbo, but can be set on the connection at startup</remarks>
        string Schema { get; }
    }
}
