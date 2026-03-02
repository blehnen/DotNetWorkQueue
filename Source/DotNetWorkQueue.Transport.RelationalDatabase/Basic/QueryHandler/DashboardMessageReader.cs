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
using System.Data;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic;

namespace DotNetWorkQueue.Transport.RelationalDatabase.Basic.QueryHandler
{
    /// <summary>
    /// Shared helper for reading a <see cref="DashboardMessage"/> from a data reader.
    /// Used by Messages, MessageDetail, and StaleMessages query handlers.
    /// </summary>
    internal static class DashboardMessageReader
    {
        /// <summary>
        /// Reads a <see cref="DashboardMessage"/> from the current row of the reader.
        /// </summary>
        /// <param name="reader">The data reader positioned on a row.</param>
        /// <param name="readColumn">The column reader.</param>
        /// <param name="commandType">The command string type for column mapping.</param>
        /// <param name="options">Transport options controlling which optional columns are present.</param>
        /// <returns>A populated <see cref="DashboardMessage"/>.</returns>
        public static DashboardMessage ReadMessage(IDataReader reader, IReadColumn readColumn,
            CommandStringTypes commandType, ITransportOptions options)
        {
            var message = new DashboardMessage();
            var columnIndex = 0;

            message.QueueId = readColumn.ReadAsInt64(commandType, columnIndex++, reader).ToString();
            message.QueuedDateTime = readColumn.ReadAsDateTimeOffset(commandType, columnIndex++, reader);
            message.CorrelationId = readColumn.ReadAsString(commandType, columnIndex++, reader);

            if (options.EnableStatus)
                message.Status = readColumn.ReadAsInt32(commandType, columnIndex++, reader);
            if (options.EnablePriority)
                message.Priority = readColumn.ReadAsInt32(commandType, columnIndex++, reader);
            if (options.EnableDelayedProcessing)
                message.QueueProcessTime = readColumn.ReadAsDateTimeOffset(commandType, columnIndex++, reader);
            if (options.EnableHeartBeat)
                message.HeartBeat = readColumn.ReadAsDateTimeOffset(commandType, columnIndex++, reader);
            if (options.EnableMessageExpiration)
                message.ExpirationTime = readColumn.ReadAsDateTimeOffset(commandType, columnIndex++, reader);
            if (options.EnableRoute)
                message.Route = readColumn.ReadAsString(commandType, columnIndex++, reader);

            return message;
        }
    }
}
