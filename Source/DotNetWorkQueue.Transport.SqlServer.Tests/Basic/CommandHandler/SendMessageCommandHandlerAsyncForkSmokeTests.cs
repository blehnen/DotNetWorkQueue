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
using System.Reflection;
using System.Runtime.CompilerServices;
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
            var sourcePath = GetHandlerSourcePath();

            Assert.IsTrue(File.Exists(sourcePath), $"Expected source at {sourcePath} not found.");
            var content = File.ReadAllText(sourcePath);
            StringAssert.Contains(content, "commandSend is RelationalSendMessageCommand",
                "HandleAsync() must guard the early branch with a type-check on RelationalSendMessageCommand.");
            StringAssert.Contains(content, "relCommand.ExternalTransaction != null",
                "HandleAsync() must null-check ExternalTransaction on the cast pattern variable.");
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
            var sourcePath = GetHandlerSourcePath();
            var content = File.ReadAllText(sourcePath).Replace("\r\n", "\n");

            // Extract the body of HandleExternalTransactionAsync by anchoring on its
            // signature and finding the matching closing brace at column 8 (method-body end).
            // The previous 6500-char window would walk past the closing brace into sibling
            // helpers, masking the actual call site if a future edit added a lifecycle call
            // to one of them.
            var forkStart = content.IndexOf("private async Task<long> HandleExternalTransactionAsync",
                StringComparison.Ordinal);
            Assert.IsTrue(forkStart >= 0, "HandleExternalTransactionAsync not found in source.");
            var forkEnd = content.IndexOf("\n        }\n", forkStart, StringComparison.Ordinal);
            Assert.IsTrue(forkEnd >= 0,
                "Closing brace of HandleExternalTransactionAsync (column-8 '}' on its own line) not found.");
            var forkBody = content.Substring(forkStart, forkEnd - forkStart);

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

        /// <summary>
        /// Returns the absolute path to the SqlServer async handler source file under test.
        /// Anchored at the test source's COMPILE-TIME location via <see cref="CallerFilePathAttribute"/>,
        /// then walks two directories up (to the test project root) and strips the
        /// <c>.Tests</c> suffix to reach the corresponding source project root. Robust to
        /// TFM changes and bin staging directories that broke the previous
        /// <c>..\..\..\..\</c> walk-up.
        /// </summary>
        private static string GetHandlerSourcePath([CallerFilePath] string testFilePath = "")
        {
            var testProjectDir = Path.GetFullPath(Path.Combine(
                Path.GetDirectoryName(testFilePath)!, "..", ".."));
            if (!testProjectDir.EndsWith(".Tests", StringComparison.Ordinal))
                throw new InvalidOperationException(
                    $"Expected test project dir '{testProjectDir}' to end with '.Tests'.");
            var sourceProjectDir = testProjectDir.Substring(0, testProjectDir.Length - ".Tests".Length);
            return Path.Combine(sourceProjectDir, "Basic", "CommandHandler", "SendMessageCommandHandlerAsync.cs");
        }
    }
}
