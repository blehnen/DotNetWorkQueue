// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2026 Brian Lehnen
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
using System.Data.Common;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.Schema
{
    /// <summary>
    /// One step in a queue's schema history.
    /// </summary>
    /// <remarks>
    /// A version produces the script that moves a queue from the version before it to its own. It does
    /// not execute anything: the updater runs the script inside the transaction and lock it already
    /// holds, so a version cannot accidentally commit half of itself.
    ///
    /// Versions are immutable once shipped. Editing one changes what a queue that already applied it
    /// believes about itself, and nothing will ever tell you - a new version is the only safe way to
    /// correct a released one.
    /// </remarks>
    public interface ISchemaVersion
    {
        /// <summary>
        /// The script that moves the schema to this version.
        /// </summary>
        /// <param name="tableNames">The table names for the queue being upgraded.</param>
        /// <param name="connection">The connection the upgrade is running on.</param>
        /// <param name="transaction">The transaction the upgrade is running in.</param>
        /// <remarks>
        /// The connection and transaction are supplied so a version can <em>read</em> to decide what to
        /// write - collapsing duplicate rows needs to know what is there. Reads must use this
        /// transaction, or they will not see what earlier versions in the same upgrade just did.
        ///
        /// Values belong in parameters and identifiers in the transport's own quoting. A migration
        /// building SQL by pasting values into text is the same defect anywhere else, and it is worse
        /// here because it runs against data nobody has looked at.
        /// </remarks>
        /// <returns>The script. Never null or empty.</returns>
        string Script(ITableNameHelper tableNames, DbConnection connection, DbTransaction transaction);
    }
}
