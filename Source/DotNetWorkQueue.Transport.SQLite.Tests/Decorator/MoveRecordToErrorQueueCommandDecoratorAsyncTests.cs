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
using System;
using System.IO;
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.Shared;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using DotNetWorkQueue.Transport.SQLite.Basic;
using DotNetWorkQueue.Transport.SQLite.Decorator;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Decorator
{
    /// <summary>
    /// The database-existence guard on the asynchronous poison-message path.
    ///
    /// SQLite is the one transport whose store can simply disappear - someone deletes the file - and
    /// the synchronous consumer declines the move rather than failing on it. The asynchronous
    /// consumer must do the same, or a deleted queue turns a poison message into a command failure
    /// and a retry cycle.
    /// </summary>
    [TestClass]
    public class MoveRecordToErrorQueueCommandDecoratorAsyncTests
    {
        [TestMethod]
        public async Task HandleAsync_WhenTheDatabaseIsGone_DeclinesTheMove()
        {
            var decorated = Substitute.For<ICommandHandlerAsync<MoveRecordToErrorQueueCommand<long>>>();
            var sut = Create(decorated, Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.db"));

            await sut.HandleAsync(null).ConfigureAwait(false);

            await decorated.DidNotReceiveWithAnyArgs().HandleAsync(null).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task HandleAsync_WhenTheDatabaseExists_PassesTheCommandThrough()
        {
            var decorated = Substitute.For<ICommandHandlerAsync<MoveRecordToErrorQueueCommand<long>>>();
            var file = Path.Combine(Path.GetTempPath(), $"present-{Guid.NewGuid():N}.db");
            File.WriteAllText(file, string.Empty);
            try
            {
                var sut = Create(decorated, file);

                await sut.HandleAsync(null).ConfigureAwait(false);

                await decorated.Received(1).HandleAsync(null).ConfigureAwait(false);
            }
            finally
            {
                try { File.Delete(file); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
        }

        [TestMethod]
        public async Task HandleAsync_WhenTheDatabaseIsInMemory_PassesTheCommandThrough()
        {
            var decorated = Substitute.For<ICommandHandlerAsync<MoveRecordToErrorQueueCommand<long>>>();
            //An in-memory database has no file to find, so the guard must not read it as missing.
            var sut = Create(decorated, fileName: null, inMemory: true);

            await sut.HandleAsync(null).ConfigureAwait(false);

            await decorated.Received(1).HandleAsync(null).ConfigureAwait(false);
        }

        private static MoveRecordToErrorQueueCommandDecoratorAsync Create(
            ICommandHandlerAsync<MoveRecordToErrorQueueCommand<long>> decorated,
            string fileName,
            bool inMemory = false)
        {
            var getFileName = Substitute.For<IGetFileNameFromConnectionString>();
            getFileName.GetFileName(Arg.Any<string>()).Returns(new ConnectionStringInfo(inMemory, fileName));

            var connectionInformation = Substitute.For<IConnectionInformation>();
            connectionInformation.ConnectionString.Returns("Data Source=whatever;Version=3;");

            return new MoveRecordToErrorQueueCommandDecoratorAsync(connectionInformation, decorated,
                new DatabaseExists(getFileName));
        }
    }
}
