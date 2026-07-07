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
using DotNetWorkQueue.Transport.Shared.Basic.Command;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SqlServer.Tests.Basic.CommandHandler
{
    /// <summary>
    /// Structural smoke tests for the SqlServer sync handler's HandleExternalTransaction fork.
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
        public void HandleExternalTransaction_PrivateMethod_ExistsWithExpectedSignature()
        {
            var handlerType = typeof(DotNetWorkQueue.Transport.SqlServer.Basic.CommandHandler.SendMessageCommandHandler);

            var method = handlerType.GetMethod("HandleExternalTransaction",
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                types: new[] { typeof(SendMessageCommand) },
                modifiers: null);

            Assert.IsNotNull(method, "HandleExternalTransaction(SendMessageCommand) must exist as a private instance method.");
            Assert.AreEqual(typeof(long), method.ReturnType, "HandleExternalTransaction must return long.");
        }

        [TestMethod]
        public void Handle_SourceContainsExternalTransactionEarlyBranch()
        {
            var sourcePath = GetHandlerSourcePath();

            Assert.IsTrue(File.Exists(sourcePath), $"Expected source at {sourcePath} not found.");
            var content = File.ReadAllText(sourcePath);
            StringAssert.Contains(content, "commandSend is RelationalSendMessageCommand",
                "Handle() must guard the early branch with a type-check on RelationalSendMessageCommand.");
            StringAssert.Contains(content, "relCommand.ExternalTransaction != null",
                "Handle() must null-check ExternalTransaction on the cast pattern variable.");
            StringAssert.Contains(content, "return HandleExternalTransaction(commandSend);",
                "Handle() must dispatch to HandleExternalTransaction on the early branch.");
            StringAssert.Contains(content, "private long HandleExternalTransaction",
                "HandleExternalTransaction must be declared private long.");
        }

        [TestMethod]
        public void HandleExternalTransaction_DoesNotCommitOrRollbackOrCloseOrDispose()
        {
            // Source-level grep guard for the lifecycle-ownership contract from PROJECT.md
            // §Success Criteria #7. The fork must NEVER call Commit/Rollback/Close/Dispose
            // on the caller's transaction or connection.
            var sourcePath = GetHandlerSourcePath();
            var content = File.ReadAllText(sourcePath).Replace("\r\n", "\n");

            // Extract the body of HandleExternalTransaction by anchoring on its signature
            // and finding the matching closing brace at column 8 (method-body end). The
            // previous 6000-char window would walk past the closing brace into sibling
            // helpers like CreateStatusRecord, masking the actual call site if a future
            // edit added a Commit/Rollback/Close/Dispose call to one of them.
            var forkStart = content.IndexOf("private long HandleExternalTransaction", StringComparison.Ordinal);
            Assert.IsGreaterThanOrEqualTo(0, forkStart, "HandleExternalTransaction not found in source.");
            var forkEnd = content.IndexOf("\n        }\n", forkStart, StringComparison.Ordinal);
            Assert.IsGreaterThanOrEqualTo(0, forkEnd,
                "Closing brace of HandleExternalTransaction (column-8 '}' on its own line) not found.");
            var forkBody = content.Substring(forkStart, forkEnd - forkStart);

            // Strip line-comments before grepping — the fork body intentionally documents the
            // contract with comments like "// Deliberately NO trans.Commit()..." which would
            // false-positive the lifecycle assertions otherwise. Real lifecycle invocations
            // are never inside comments.
            var lines = forkBody.Split('\n');
            var codeOnly = new System.Text.StringBuilder();
            foreach (var line in lines)
            {
                var trimmed = line.TrimStart();
                if (trimmed.StartsWith("//", StringComparison.Ordinal))
                    continue;
                // Strip trailing line-comments from a code line (rough but adequate here).
                var commentIdx = line.IndexOf("//", StringComparison.Ordinal);
                codeOnly.AppendLine(commentIdx >= 0 ? line.Substring(0, commentIdx) : line);
            }
            var forkCode = codeOnly.ToString();

            Assert.DoesNotContain(".Commit()", forkCode,    "HandleExternalTransaction must not call .Commit() on the caller's transaction.");
            Assert.DoesNotContain(".Rollback()", forkCode,  "HandleExternalTransaction must not call .Rollback() on the caller's transaction.");
            // Close and Dispose are looked for as method invocations on conn/transaction — broad enough
            // to catch sqlConn.Close(), sqlTransaction.Dispose(), etc. False-positives unlikely because
            // the fork body has no other Close/Dispose surface.
            Assert.DoesNotContain(".Close()", forkCode,     "HandleExternalTransaction must not call .Close() on the caller's connection.");
            Assert.DoesNotContain(".Dispose()", forkCode,   "HandleExternalTransaction must not call .Dispose() on the caller's connection or transaction.");
        }

        /// <summary>
        /// Returns the absolute path to the SqlServer handler source file under test.
        /// Walks up from the test assembly's runtime directory until it finds a parent
        /// whose name ends with <c>.Tests</c>; that's the test project root. Strips the
        /// <c>.Tests</c> suffix to reach the corresponding source project root, then
        /// appends the handler's relative path.
        /// </summary>
        /// <remarks>
        /// An earlier revision used <c>[CallerFilePath]</c> for compile-time path anchoring,
        /// but CI builds with <c>ContinuousIntegrationBuild=true</c> (set automatically by
        /// <c>Directory.Build.props</c> when the <c>CI</c> env var is present) rewrite
        /// source paths to the deterministic placeholder <c>/_/Source/...</c>, which doesn't
        /// exist on disk at test runtime. Walking the assembly's <em>runtime</em> directory
        /// avoids that pitfall while still adapting to any TFM (no hardcoded depth).
        /// </remarks>
        private static string GetHandlerSourcePath()
        {
            var dir = new DirectoryInfo(
                Path.GetDirectoryName(typeof(SendMessageCommandHandlerForkSmokeTests).Assembly.Location)!);
            while (dir != null && !dir.Name.EndsWith(".Tests", StringComparison.Ordinal))
                dir = dir.Parent;
            if (dir == null)
                throw new InvalidOperationException(
                    "Could not find a parent directory ending in '.Tests' from the test assembly's path.");
            var sourceProjectDir = dir.FullName.Substring(0, dir.FullName.Length - ".Tests".Length);
            return Path.Combine(sourceProjectDir, "Basic", "CommandHandler", "SendMessageCommandHandler.cs");
        }
    }
}
