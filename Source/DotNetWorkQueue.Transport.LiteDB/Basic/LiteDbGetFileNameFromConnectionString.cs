﻿// ---------------------------------------------------------------------
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
    /// Determines what the file name is from the connection string
    /// </summary>
    /// <seealso cref="DotNetWorkQueue.Transport.LiteDb.IGetFileNameFromConnectionString" />
    internal class LiteDbGetFileNameFromConnectionString: IGetFileNameFromConnectionString
    {
        public ConnectionStringInfo GetFileName(string connectionString)
        {
            Guard.NotNullOrEmpty(() => connectionString, connectionString);
            var connection = new ConnectionString(connectionString);

            if (string.IsNullOrWhiteSpace(connection.Filename)) return null;

            if (connection.Filename == ":memory")
                throw new NotSupportedException(
                    "Memory based databases are not supported, as they don't travel between connections");
            return new ConnectionStringInfo(connection.Filename);
        }
    }
}
