// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using DotNetWorkQueue.Messages;
namespace DotNetWorkQueue.Transport.SqlServer.Basic
{
    /// <summary>
    /// The correlation ID for the SQL server queue
    /// <remarks>A GUID is used, since this needs to be different accross queues</remarks>
    /// </summary>
    public class SqlServerMessageQueueCorrelationId: ICorrelationId
    {
        private Guid _id;
        /// <summary>
        /// Initializes a new instance of the <see cref="SqlServerMessageQueueCorrelationId"/> class.
        /// </summary>
        /// <param name="id">The identifier.</param>
        public SqlServerMessageQueueCorrelationId(Guid id)
        {
            _id = id;
            Id = new Setting<Guid>(id);
        }
        /// <summary>
        /// Gets or sets the identifier.
        /// </summary>
        /// <value>
        /// The identifier.
        /// </value>
        public ISetting Id
        {
            get;
            set;
        }
        /// <summary>
        /// Gets a value indicating whether <see cref="Id" /> has a non-null/non-empty value.
        /// </summary>
        /// <value>
        /// <c>true</c> if the <see cref="Id" /> is not null/empty, otherwise <c>false</c>.
        /// </value>
        public bool HasValue => _id != Guid.Empty;

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return _id.ToString();
        }
    }
}
