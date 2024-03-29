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

// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.
//
// To add a suppression to this file, right-click the message in the 
// Code Analysis results, point to "Suppress Message", and click 
// "In Suppression File".
// You do not need to add suppressions to this file manually.

#region Using

using System.Diagnostics.CodeAnalysis;

#endregion

[assembly: SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Scope = "member", Target = "DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler.DeleteQueueTablesCommandHandler.#HandleAsync(DotNetWorkQueue.Transport.SqlServer.Basic.Command.DeleteQueueTablesCommand)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Scope = "member", Target = "DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler.DeleteStatusTableStatusCommandHandler.#HandleAsync(DotNetWorkQueue.Transport.SqlServer.Basic.Command.DeleteStatusTableStatusCommand)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Scope = "member", Target = "DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler.DeleteTransactionalMessageCommandHandler.#HandleAsync(DotNetWorkQueue.Transport.SqlServer.Basic.Command.DeleteTransactionalMessageCommand)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Scope = "member", Target = "DotNetWorkQueue.Transport.SqlServer.Basic.QueryHandler.FindExpiredRecordsToDeleteQueryHandler.#HandleAsync(DotNetWorkQueue.Transport.SqlServer.Basic.Query.FindExpiredMessagesToDeleteQuery)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Scope = "member", Target = "DotNetWorkQueue.Transport.SqlServer.Basic.QueryHandler.FindRecordsToResetByHeartBeatQueryHandler.#HandleAsync(DotNetWorkQueue.Transport.SqlServer.Basic.Query.FindMessagesToResetByHeartBeatQuery)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Scope = "member", Target = "DotNetWorkQueue.Transport.SqlServer.Basic.QueryHandler.GetColumnNamesFromTableQueryHandler.#HandleAsync(DotNetWorkQueue.Transport.SqlServer.Basic.Query.GetColumnNamesFromTableQuery)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Scope = "member", Target = "DotNetWorkQueue.Transport.SqlServer.Basic.QueryHandler.GetErrorRecordExistsQueryHandler.#HandleAsync(DotNetWorkQueue.Transport.SqlServer.Basic.Query.GetErrorRecordExistsQuery)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Scope = "member", Target = "DotNetWorkQueue.Transport.SqlServer.Basic.QueryHandler.GetErrorRetryCountQueryHandler.#HandleAsync(DotNetWorkQueue.Transport.SqlServer.Basic.Query.GetErrorRetryCountQuery)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Scope = "member", Target = "DotNetWorkQueue.Transport.SqlServer.Basic.QueryHandler.GetQueueOptionsQueryHandler.#HandleAsync(DotNetWorkQueue.Transport.SqlServer.Basic.Query.GetQueueOptionsQuery)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Scope = "member", Target = "DotNetWorkQueue.Transport.SqlServer.Basic.QueryHandler.GetTableExistsQueryHandler.#HandleAsync(DotNetWorkQueue.Transport.SqlServer.Basic.Query.GetTableExistsQuery)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Scope = "member", Target = "DotNetWorkQueue.Transport.SqlServer.Basic.QueryHandler.GetUtcDateQueryHandler.#HandleAsync(DotNetWorkQueue.Transport.SqlServer.Basic.Query.GetUtcDateQuery)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Scope = "member", Target = "DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler.RollbackMessageCommandHandler.#HandleAsync(DotNetWorkQueue.Transport.SqlServer.Basic.Command.RollbackMessageCommand)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Scope = "member", Target = "DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler.SendHeartBeatCommandHandler.#HandleAsync(DotNetWorkQueue.Transport.SqlServer.Basic.Command.SendHeartBeatCommand)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Scope = "member", Target = "DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler.SendMessageCommandHandler`1.#CreateStatusRecord(System.Data.SqlClient.SqlConnection,System.Int64,DotNetWorkQueue.IMessage,DotNetWorkQueue.Transport.SqlServer.Basic.SqlServerMessageQueueAdditionalMessageData)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Scope = "member", Target = "DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler.SetErrorCountCommandHandler.#HandleAsync(DotNetWorkQueue.Transport.SqlServer.Basic.Command.SetErrorCountCommand)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Scope = "member", Target = "DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler.SetStatusTableStatusCommandHandler.#HandleAsync(DotNetWorkQueue.Transport.SqlServer.Basic.Command.SetStatusTableStatusCommand)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Scope = "member", Target = "DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler.MoveRecordToErrorQueueCommandHandler.#HandleAsync(DotNetWorkQueue.Transport.SqlServer.Basic.Command.MoveRecordToErrorQueueCommand)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Scope = "member", Target = "DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler.MoveRecordToErrorQueueCommandHandler.#HandleForTransaction(DotNetWorkQueue.Transport.SqlServer.Basic.Command.MoveRecordToErrorQueueCommand)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Scope = "member", Target = "DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler.SendMessageCommandHandler`1.#HandleAsync(DotNetWorkQueue.Transport.SqlServer.Basic.Command.SendMessageCommand`1<!0>)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Scope = "member", Target = "DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler.SendMessageCommandHandler`1.#CreateMetaDataRecord(System.Nullable`1<System.TimeSpan>,System.TimeSpan,System.Data.SqlClient.SqlConnection,System.Int64,DotNetWorkQueue.IMessage,DotNetWorkQueue.Transport.SqlServer.Basic.SqlServerMessageQueueAdditionalMessageData)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Scope = "member", Target = "DotNetWorkQueue.Transport.SqlServer.Basic.QueryHandler.ReceiveMessageQueryHandler.#HandleAsync(DotNetWorkQueue.Transport.SqlServer.Basic.Query.ReceiveMessageQuery)")]
[assembly: SuppressMessage("Microsoft.Security", "CA2100:Review SQL queries for security vulnerabilities", Scope = "member", Target = "DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler.ResetHeartBeatCommandHandler.#HandleAsync(DotNetWorkQueue.Transport.SqlServer.Basic.Command.ResetHeartBeatCommand)")]
