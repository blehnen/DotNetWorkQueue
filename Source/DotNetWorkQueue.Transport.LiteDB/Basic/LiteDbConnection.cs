// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2021 Brian Lehnen
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
using System;
using DotNetWorkQueue.Validation;
using LiteDB;

namespace DotNetWorkQueue.Transport.LiteDb.Basic
{
    /// <summary>
    /// Wraps a LiteDatabase. If direct/memory will leave the instance alone on dispose. If shared, database will be disposed.
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class LiteDbConnection: IDisposable
    {
        private readonly bool _shared;

        /// <summary>
        /// Initializes a new instance of the <see cref="LiteDbConnection"/> class.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="shared">if set to <c>true</c> [shared].</param>
        public LiteDbConnection(LiteDatabase database, bool shared)
        {
            Guard.NotNull(() => database, database);
            Database = database;
            _shared = shared;
        }

        /// <summary>
        /// Gets the database.
        /// </summary>
        /// <value>
        /// The database.
        /// </value>
        public LiteDatabase Database { get; }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if(_shared) //only dispose shared connections; manager handles direct
                Database?.Dispose();
        }
    }
}
