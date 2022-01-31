// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
namespace DotNetWorkQueue.Transport.LiteDb.Basic
{
    /// <summary>
    /// Defines our collections that hold the data for the queue
    /// </summary>
    public interface ITable
    {
        /// <summary>
        /// Creates this table in the database specified by the connection
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="options">The options.</param>
        /// <param name="helper">The helper.</param>
        /// <returns></returns>
        bool Create(LiteDbConnectionManager connection, LiteDbMessageQueueTransportOptions options, TableNameHelper helper);
    }
}