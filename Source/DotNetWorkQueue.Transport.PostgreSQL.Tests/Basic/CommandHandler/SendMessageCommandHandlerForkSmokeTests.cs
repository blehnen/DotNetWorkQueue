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
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.PostgreSQL.Tests.Basic.CommandHandler
{
    /// <summary>
    /// Structural smoke tests for the PostgreSQL sync handler's HandleExternalTx fork.
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
        /// Verifies that <c>HandleExternalTx(SendMessageCommand)</c> exists as a private
        /// instance method on the PostgreSQL <c>SendMessageCommandHandler</c> with return
        /// type <see cref="long"/>.
        /// </summary>
        [TestMethod]
        public void HandleExternalTx_PrivateMethod_ExistsWithExpectedSignature()
        {
            var handlerType = typeof(DotNetWorkQueue.Transport.PostgreSQL.Basic.CommandHandler.SendMessageCommandHandler);

            var method = handlerType.GetMethod("HandleExternalTx",
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(SendMessageCommand) },
                modifiers: null);

            Assert.IsNotNull(method, "HandleExternalTx(SendMessageCommand) must exist as a private instance method.");
            Assert.AreEqual(typeof(long), method.ReturnType, "HandleExternalTx must return long.");
        }

        /// <summary>
        /// Verifies that <c>Handle()</c> contains the early-branch dispatch to
        /// <c>HandleExternalTx</c> when <c>ExternalTransaction</c> is non-null, and that
        /// the private <c>HandleExternalTx</c> method is declared with the expected
        /// signature in the source file.
        /// </summary>
        [TestMethod]
        public void Handle_SourceContainsExternalTransactionEarlyBranch()
        {
            // Resolve the source file relative to the test bin output. dotnet test runs
            // from the project's bin directory; the source file is 4 levels up + into
            // the main PG project's Basic/CommandHandler folder.
            var sourcePath = Path.Combine(
                Path.GetDirectoryName(typeof(SendMessageCommandHandlerForkSmokeTests).Assembly.Location)!,
                "..", "..", "..", "..",
                "DotNetWorkQueue.Transport.PostgreSQL",
                "Basic", "CommandHandler",
                "SendMessageCommandHandler.cs");
            sourcePath = Path.GetFullPath(sourcePath);

            Assert.IsTrue(File.Exists(sourcePath), $"Expected source at {sourcePath} not found.");
            var content = File.ReadAllText(sourcePath);
            StringAssert.Contains(content, "commandSend.ExternalTransaction != null",
                "Handle() must contain the early-branch null-check on ExternalTransaction.");
            StringAssert.Contains(content, "return HandleExternalTx(commandSend);",
                "Handle() must dispatch to HandleExternalTx on the early branch.");
            StringAssert.Contains(content, "private long HandleExternalTx",
                "HandleExternalTx must be declared private long.");
        }

        /// <summary>
        /// Source-level grep guard for the lifecycle-ownership contract from PROJECT.md
        /// §Success Criteria #7. The fork must NEVER call Commit/Rollback/Close/Dispose
        /// on the caller's transaction or connection.
        /// </summary>
        [TestMethod]
        public void HandleExternalTx_DoesNotCommitOrRollbackOrCloseOrDispose()
        {
            // CONTEXT-4 Rule B mandates the lifecycle comment uses word forms
            // ("no Commit, Rollback, Close, or Dispose") so plain substring search is
            // safe — no preprocessing needed.
            var sourcePath = Path.Combine(
                Path.GetDirectoryName(typeof(SendMessageCommandHandlerForkSmokeTests).Assembly.Location)!,
                "..", "..", "..", "..",
                "DotNetWorkQueue.Transport.PostgreSQL",
                "Basic", "CommandHandler",
                "SendMessageCommandHandler.cs");
            sourcePath = Path.GetFullPath(sourcePath);

            var content = File.ReadAllText(sourcePath);
            // Extract the body of HandleExternalTx by anchoring on its signature.
            // Conservative end-bound: 6000 chars forward (fork is ~80 lines, plenty).
            var forkStart = content.IndexOf("private long HandleExternalTx",
                System.StringComparison.Ordinal);
            Assert.IsTrue(forkStart >= 0, "HandleExternalTx not found in source.");
            var forkBody = content.Substring(forkStart, System.Math.Min(6000, content.Length - forkStart));

            Assert.IsFalse(forkBody.Contains(".Commit()"), "HandleExternalTx must not call .Commit() on the caller's transaction.");
            Assert.IsFalse(forkBody.Contains(".Rollback()"), "HandleExternalTx must not call .Rollback() on the caller's transaction.");
            Assert.IsFalse(forkBody.Contains(".Close()"), "HandleExternalTx must not call .Close() on the caller's connection.");
            Assert.IsFalse(forkBody.Contains(".Dispose()"), "HandleExternalTx must not call .Dispose() on the caller's connection or transaction.");
        }
    }
}
