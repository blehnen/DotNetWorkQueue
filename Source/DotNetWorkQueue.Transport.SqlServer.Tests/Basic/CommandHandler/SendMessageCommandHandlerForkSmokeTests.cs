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

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Basic.CommandHandler
{
    /// <summary>
    /// Structural smoke tests for the SqlServer sync handler's HandleExternalTx fork.
    /// Per RESEARCH §11 Discrepancy #2 + CLAUDE.md sync-vs-async mocking lesson, direct
    /// execution tests of the fork are infeasible at the unit-test level (sealed
    /// SqlConnection/SqlTransaction/SqlCommand types) and live in Phase 6 integration
    /// tests against a real SqlServer instance. This test verifies only the structural
    /// shape of the fork: it exists, has the expected signature, and is invoked by Handle().
    /// </summary>
    [TestClass]
    public class SendMessageCommandHandlerForkSmokeTests
    {
        [TestMethod]
        public void HandleExternalTx_PrivateMethod_ExistsWithExpectedSignature()
        {
            var handlerType = typeof(DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler.SendMessageCommandHandler);

            var method = handlerType.GetMethod("HandleExternalTx",
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(SendMessageCommand) },
                modifiers: null);

            Assert.IsNotNull(method, "HandleExternalTx(SendMessageCommand) must exist as a private instance method.");
            Assert.AreEqual(typeof(long), method.ReturnType, "HandleExternalTx must return long.");
        }

        [TestMethod]
        public void Handle_SourceContainsExternalTransactionEarlyBranch()
        {
            // Read SendMessageCommandHandler.cs from the source tree relative to the test
            // bin output. dotnet test runs from the project's bin directory; the source
            // file is 4 levels up + into the main project's Basic/CommandHandler folder.
            var sourcePath = Path.Combine(
                Path.GetDirectoryName(typeof(SendMessageCommandHandlerForkSmokeTests).Assembly.Location)!,
                "..", "..", "..", "..",
                "DotNetWorkQueue.Transport.SqlServer",
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

        [TestMethod]
        public void HandleExternalTx_DoesNotCommitOrRollbackOrCloseOrDispose()
        {
            // Source-level grep guard for the lifecycle-ownership contract from PROJECT.md
            // §Success Criteria #7. The fork must NEVER call Commit/Rollback/Close/Dispose
            // on the caller's transaction or connection.
            var sourcePath = Path.Combine(
                Path.GetDirectoryName(typeof(SendMessageCommandHandlerForkSmokeTests).Assembly.Location)!,
                "..", "..", "..", "..",
                "DotNetWorkQueue.Transport.SqlServer",
                "Basic", "CommandHandler",
                "SendMessageCommandHandler.cs");
            sourcePath = Path.GetFullPath(sourcePath);

            var content = File.ReadAllText(sourcePath);
            // Extract the body of HandleExternalTx by anchoring on its signature and the
            // closing-brace of the method (the next "        }" at column 8 after its body).
            var forkStart = content.IndexOf("private long HandleExternalTx", System.StringComparison.Ordinal);
            Assert.IsTrue(forkStart >= 0, "HandleExternalTx not found in source.");
            // Conservative end-bound: search 6000 chars forward (the fork is ~80 lines, plenty).
            var forkBody = content.Substring(forkStart, System.Math.Min(6000, content.Length - forkStart));

            // Strip line-comments before grepping — the fork body intentionally documents the
            // contract with comments like "// Deliberately NO trans.Commit()..." which would
            // false-positive the lifecycle assertions otherwise. Real lifecycle invocations
            // are never inside comments.
            var lines = forkBody.Split('\n');
            var codeOnly = new System.Text.StringBuilder();
            foreach (var line in lines)
            {
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith("//", System.StringComparison.Ordinal))
                    continue;
                // Strip trailing line-comments from a code line (rough but adequate here).
                var commentIdx = line.IndexOf("//", System.StringComparison.Ordinal);
                codeOnly.AppendLine(commentIdx >= 0 ? line.Substring(0, commentIdx) : line);
            }
            var forkCode = codeOnly.ToString();

            Assert.IsFalse(forkCode.Contains(".Commit()"),    "HandleExternalTx must not call .Commit() on the caller's transaction.");
            Assert.IsFalse(forkCode.Contains(".Rollback()"),  "HandleExternalTx must not call .Rollback() on the caller's transaction.");
            // Close and Dispose are looked for as method invocations on conn/tx — broad enough
            // to catch sqlConn.Close(), sqlTx.Dispose(), etc. False-positives unlikely because
            // the fork body has no other Close/Dispose surface.
            Assert.IsFalse(forkCode.Contains(".Close()"),     "HandleExternalTx must not call .Close() on the caller's connection.");
            Assert.IsFalse(forkCode.Contains(".Dispose()"),   "HandleExternalTx must not call .Dispose() on the caller's connection or transaction.");
        }
    }
}
