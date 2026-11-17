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
    /// The database-existence guard on the asynchronous commit, error-count and status-table commands.
    ///
    /// The synchronous consumer declines each of these when the database file has been deleted. The
    /// asynchronous one must do the same, or it runs them against a missing database and retries.
    /// The in-memory case is covered by <see cref="MoveRecordToErrorQueueCommandDecoratorAsyncTests"/>;
    /// all four share <see cref="DatabaseExists"/>.
    /// </summary>
    [TestClass]
    public class DatabaseExistsDecoratorAsyncTests
    {
        [TestMethod]
        public async Task DeleteMessage_WhenTheDatabaseIsGone_DeletesNothing()
        {
            var decorated = Substitute.For<ICommandHandlerWithOutputAsync<DeleteMessageCommand<long>, long>>();
            var sut = new DeleteMessageCommandDecoratorAsync(Connection(), decorated, Exists(MissingFile()));

            Assert.AreEqual(0, await sut.HandleAsync(new DeleteMessageCommand<long>(1)).ConfigureAwait(false));
            await decorated.DidNotReceiveWithAnyArgs().HandleAsync(null).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeleteMessage_WhenTheDatabaseExists_ReturnsTheDecoratedResult()
        {
            var decorated = Substitute.For<ICommandHandlerWithOutputAsync<DeleteMessageCommand<long>, long>>();
            var command = new DeleteMessageCommand<long>(1);
            decorated.HandleAsync(command).Returns(Task.FromResult(3L));

            await WithExistingFile(async file =>
            {
                var sut = new DeleteMessageCommandDecoratorAsync(Connection(), decorated, Exists(file));

                Assert.AreEqual(3, await sut.HandleAsync(command).ConfigureAwait(false));
                await decorated.Received(1).HandleAsync(command).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task SetErrorCount_WhenTheDatabaseIsGone_DeclinesTheCommand()
        {
            var decorated = Substitute.For<ICommandHandlerAsync<SetErrorCountCommand<long>>>();
            var sut = new SetErrorCountCommandDecoratorAsync(Connection(), decorated, Exists(MissingFile()));

            await sut.HandleAsync(new SetErrorCountCommand<long>("System.Exception", 1, 1)).ConfigureAwait(false);

            await decorated.DidNotReceiveWithAnyArgs().HandleAsync(null).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task SetErrorCount_WhenTheDatabaseExists_PassesTheCommandThrough()
        {
            var decorated = Substitute.For<ICommandHandlerAsync<SetErrorCountCommand<long>>>();
            var command = new SetErrorCountCommand<long>("System.Exception", 1, 1);

            await WithExistingFile(async file =>
            {
                var sut = new SetErrorCountCommandDecoratorAsync(Connection(), decorated, Exists(file));

                await sut.HandleAsync(command).ConfigureAwait(false);

                await decorated.Received(1).HandleAsync(command).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeleteStatusTableStatus_WhenTheDatabaseIsGone_DeclinesTheCommand()
        {
            var decorated = Substitute.For<ICommandHandlerAsync<DeleteStatusTableStatusCommand<long>>>();
            var sut = new SetStatusTableStatusCommandDecoratorAsync(Connection(), decorated, Exists(MissingFile()));

            await sut.HandleAsync(new DeleteStatusTableStatusCommand<long>(1)).ConfigureAwait(false);

            await decorated.DidNotReceiveWithAnyArgs().HandleAsync(null).ConfigureAwait(false);
        }

        [TestMethod]
        public async Task DeleteStatusTableStatus_WhenTheDatabaseExists_PassesTheCommandThrough()
        {
            var decorated = Substitute.For<ICommandHandlerAsync<DeleteStatusTableStatusCommand<long>>>();
            var command = new DeleteStatusTableStatusCommand<long>(1);

            await WithExistingFile(async file =>
            {
                var sut = new SetStatusTableStatusCommandDecoratorAsync(Connection(), decorated, Exists(file));

                await sut.HandleAsync(command).ConfigureAwait(false);

                await decorated.Received(1).HandleAsync(command).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        private static string MissingFile() => Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.db");

        private static async Task WithExistingFile(Func<string, Task> test)
        {
            var file = Path.Combine(Path.GetTempPath(), $"present-{Guid.NewGuid():N}.db");
            File.WriteAllText(file, string.Empty);
            try
            {
                await test(file).ConfigureAwait(false);
            }
            finally
            {
                try { File.Delete(file); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
        }

        private static DatabaseExists Exists(string fileName)
        {
            var getFileName = Substitute.For<IGetFileNameFromConnectionString>();
            getFileName.GetFileName(Arg.Any<string>()).Returns(new ConnectionStringInfo(false, fileName));
            return new DatabaseExists(getFileName);
        }

        private static IConnectionInformation Connection()
        {
            var connectionInformation = Substitute.For<IConnectionInformation>();
            connectionInformation.ConnectionString.Returns("Data Source=whatever;Version=3;");
            return connectionInformation;
        }
    }
}
