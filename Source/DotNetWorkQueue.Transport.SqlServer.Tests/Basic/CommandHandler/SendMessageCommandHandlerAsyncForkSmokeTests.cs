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
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Basic.CommandHandler
{
    /// <summary>
    /// Structural smoke tests for the SqlServer async handler's HandleExternalTransactionAsync fork.
    /// Per RESEARCH §11 Discrepancy #2 + CLAUDE.md sync-vs-async mocking lesson, direct
    /// execution tests are infeasible at the unit-test level and live in Phase 6
    /// integration tests against a real SqlServer instance.
    /// </summary>
    [TestClass]
    public class SendMessageCommandHandlerAsyncForkSmokeTests
    {
        [TestMethod]
        public void HandleExternalTransactionAsync_PrivateMethod_ExistsWithExpectedSignature()
        {
            var handlerType = typeof(DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler.SendMessageCommandHandlerAsync);

            var method = handlerType.GetMethod("HandleExternalTransactionAsync",
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(SendMessageCommand) },
                modifiers: null);

            Assert.IsNotNull(method, "HandleExternalTransactionAsync(SendMessageCommand) must exist as a private instance method.");
            Assert.AreEqual(typeof(Task<long>), method.ReturnType, "HandleExternalTransactionAsync must return Task<long>.");
        }

        [TestMethod]
        public void HandleAsync_SourceContainsExternalTransactionEarlyBranch()
        {
            var sourcePath = Path.Combine(
                Path.GetDirectoryName(typeof(SendMessageCommandHandlerAsyncForkSmokeTests).Assembly.Location)!,
                "..", "..", "..", "..",
                "DotNetWorkQueue.Transport.SqlServer",
                "Basic", "CommandHandler",
                "SendMessageCommandHandlerAsync.cs");
            sourcePath = Path.GetFullPath(sourcePath);

            Assert.IsTrue(File.Exists(sourcePath), $"Expected source at {sourcePath} not found.");
            var content = File.ReadAllText(sourcePath);
            StringAssert.Contains(content, "commandSend.ExternalTransaction != null",
                "HandleAsync() must contain the early-branch null-check on ExternalTransaction.");
            StringAssert.Contains(content, "HandleExternalTransactionAsync(commandSend)",
                "HandleAsync() must dispatch to HandleExternalTransactionAsync on the early branch.");
            StringAssert.Contains(content, "private async Task<long> HandleExternalTransactionAsync",
                "HandleExternalTransactionAsync must be declared private async Task<long>.");
            StringAssert.Contains(content, "await HandleExternalTransactionAsync(commandSend).ConfigureAwait(false)",
                "The early-branch must await with ConfigureAwait(false) consistent with the handler's await style.");
        }

        [TestMethod]
        public void HandleExternalTransactionAsync_DoesNotCommitOrRollbackOrCloseOrDispose()
        {
            var sourcePath = Path.Combine(
                Path.GetDirectoryName(typeof(SendMessageCommandHandlerAsyncForkSmokeTests).Assembly.Location)!,
                "..", "..", "..", "..",
                "DotNetWorkQueue.Transport.SqlServer",
                "Basic", "CommandHandler",
                "SendMessageCommandHandlerAsync.cs");
            sourcePath = Path.GetFullPath(sourcePath);

            var content = File.ReadAllText(sourcePath);
            var forkStart = content.IndexOf("private async Task<long> HandleExternalTransactionAsync",
                System.StringComparison.Ordinal);
            Assert.IsTrue(forkStart >= 0, "HandleExternalTransactionAsync not found in source.");
            var forkBody = content.Substring(forkStart, System.Math.Min(6500, content.Length - forkStart));

            Assert.IsFalse(forkBody.Contains(".Commit()"),   "HandleExternalTransactionAsync must not call .Commit() on the caller's transaction.");
            Assert.IsFalse(forkBody.Contains(".Rollback()"), "HandleExternalTransactionAsync must not call .Rollback() on the caller's transaction.");
            Assert.IsFalse(forkBody.Contains(".Close()"),    "HandleExternalTransactionAsync must not call .Close() on the caller's connection.");
            Assert.IsFalse(forkBody.Contains(".Dispose()"),  "HandleExternalTransactionAsync must not call .Dispose() on the caller's connection or transaction.");
            // Async-specific lifecycle calls:
            Assert.IsFalse(forkBody.Contains(".CommitAsync"),   "HandleExternalTransactionAsync must not call .CommitAsync on the caller's transaction.");
            Assert.IsFalse(forkBody.Contains(".RollbackAsync"), "HandleExternalTransactionAsync must not call .RollbackAsync on the caller's transaction.");
            Assert.IsFalse(forkBody.Contains(".CloseAsync"),    "HandleExternalTransactionAsync must not call .CloseAsync on the caller's connection.");
            Assert.IsFalse(forkBody.Contains(".DisposeAsync"),  "HandleExternalTransactionAsync must not call .DisposeAsync on the caller's connection or transaction.");
        }
    }
}
