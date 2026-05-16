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
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Basic.CommandHandler
{
    /// <summary>
    /// Structural smoke tests for the PostgreSQL sync handler's HandleExternalTransaction fork.
    /// Per RESEARCH §5 + CLAUDE.md sync-vs-async mocking lesson, direct execution tests
    /// of the fork are infeasible at the unit-test level (sealed NpgsqlConnection /
    /// NpgsqlTransaction / NpgsqlCommand types) and live in Phase 6 integration tests
    /// against a real PostgreSQL instance. This test verifies only the structural shape
    /// of the fork: it exists, has the expected signature, and is invoked by Handle().
    /// </summary>
    [TestClass]
    public class SendMessageCommandHandlerForkSmokeTests
    {
        /// <summary>
        /// Verifies that <c>HandleExternalTransaction(SendMessageCommand)</c> exists as a private
        /// instance method on the PostgreSQL <c>SendMessageCommandHandler</c> with return
        /// type <see cref="long"/>.
        /// </summary>
        [TestMethod]
        public void HandleExternalTransaction_PrivateMethod_ExistsWithExpectedSignature()
        {
            var handlerType = typeof(DotNetWorkQueue.Transport.PostgreSQL.Basic.CommandHandler.SendMessageCommandHandler);

            var method = handlerType.GetMethod("HandleExternalTransaction",
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(SendMessageCommand) },
                modifiers: null);

            Assert.IsNotNull(method, "HandleExternalTransaction(SendMessageCommand) must exist as a private instance method.");
            Assert.AreEqual(typeof(long), method.ReturnType, "HandleExternalTransaction must return long.");
        }

        /// <summary>
        /// Verifies that <c>Handle()</c> contains the early-branch dispatch to
        /// <c>HandleExternalTransaction</c> when <c>ExternalTransaction</c> is non-null, and that
        /// the private <c>HandleExternalTransaction</c> method is declared with the expected
        /// signature in the source file.
        /// </summary>
        [TestMethod]
        public void Handle_SourceContainsExternalTransactionEarlyBranch()
        {
            var sourcePath = GetHandlerSourcePath();

            Assert.IsTrue(File.Exists(sourcePath), $"Expected source at {sourcePath} not found.");
            var content = File.ReadAllText(sourcePath);
            StringAssert.Contains(content, "commandSend.ExternalTransaction != null",
                "Handle() must contain the early-branch null-check on ExternalTransaction.");
            StringAssert.Contains(content, "return HandleExternalTransaction(commandSend);",
                "Handle() must dispatch to HandleExternalTransaction on the early branch.");
            StringAssert.Contains(content, "private long HandleExternalTransaction",
                "HandleExternalTransaction must be declared private long.");
        }

        /// <summary>
        /// Source-level grep guard for the lifecycle-ownership contract from PROJECT.md
        /// §Success Criteria #7. The fork must NEVER call Commit/Rollback/Close/Dispose
        /// on the caller's transaction or connection.
        /// </summary>
        [TestMethod]
        public void HandleExternalTransaction_DoesNotCommitOrRollbackOrCloseOrDispose()
        {
            // CONTEXT-4 Rule B mandates the lifecycle comment uses word forms
            // ("no Commit, Rollback, Close, or Dispose") so plain substring search is
            // safe — no comment-stripping needed.
            var sourcePath = GetHandlerSourcePath();
            var content = File.ReadAllText(sourcePath).Replace("\r\n", "\n");

            // Extract the body of HandleExternalTransaction by anchoring on its signature
            // and finding the matching closing brace at column 8 (method-body end). The
            // previous 6000-char window would walk past the closing brace into sibling
            // helpers, masking the actual call site if a future edit added a Commit /
            // Rollback / Close / Dispose call to one of them.
            var forkStart = content.IndexOf("private long HandleExternalTransaction", StringComparison.Ordinal);
            Assert.IsTrue(forkStart >= 0, "HandleExternalTransaction not found in source.");
            var forkEnd = content.IndexOf("\n        }\n", forkStart, StringComparison.Ordinal);
            Assert.IsTrue(forkEnd >= 0,
                "Closing brace of HandleExternalTransaction (column-8 '}' on its own line) not found.");
            var forkBody = content.Substring(forkStart, forkEnd - forkStart);

            Assert.IsFalse(forkBody.Contains(".Commit()"), "HandleExternalTransaction must not call .Commit() on the caller's transaction.");
            Assert.IsFalse(forkBody.Contains(".Rollback()"), "HandleExternalTransaction must not call .Rollback() on the caller's transaction.");
            Assert.IsFalse(forkBody.Contains(".Close()"), "HandleExternalTransaction must not call .Close() on the caller's connection.");
            Assert.IsFalse(forkBody.Contains(".Dispose()"), "HandleExternalTransaction must not call .Dispose() on the caller's connection or transaction.");
        }

        /// <summary>
        /// Returns the absolute path to the PostgreSQL handler source file under test.
        /// Anchored at the test source's COMPILE-TIME location via <see cref="CallerFilePathAttribute"/>,
        /// then walks two directories up (to the test project root) and strips the
        /// <c>.Tests</c> suffix to reach the corresponding source project root.
        /// Robust to TFM changes and bin staging directories that broke the previous
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
            return Path.Combine(sourceProjectDir, "Basic", "CommandHandler", "SendMessageCommandHandler.cs");
        }
    }
}
