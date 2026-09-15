using System;
using System.Collections.Generic;
using System.IO;

namespace DotNetWorkQueue.IntegrationTests.Shared
{
    /// <summary>
    /// Reads an upgrade script out of docs/upgrade, so the tests exercise the file that ships rather
    /// than a copy of it.
    /// </summary>
    /// <remarks>
    /// A copy would prove nothing about what an operator is handed: the two drift the moment either is
    /// edited alone, and the test would keep passing. Reading the real file means an edit to the script
    /// that breaks it fails the build.
    ///
    /// The placeholder substitution is deliberately the same edit the scripts tell an operator to make -
    /// a find-and-replace of YourQueueName - so the documented procedure is what gets tested.
    ///
    /// GitHub #321.
    /// </remarks>
    public static class UpgradeScript
    {
        private const string QueueNamePlaceholder = "YourQueueName";

        /// <summary>
        /// Reads a script for the 0.12.0 upgrade, with the queue name filled in.
        /// </summary>
        /// <param name="fileName">File name inside docs/upgrade/0.12.0, e.g. "sqlite.sql".</param>
        /// <param name="queueName">The queue the script should target.</param>
        public static string Read(string fileName, string queueName)
        {
            return Read(fileName, queueName, null);
        }

        /// <summary>
        /// Reads a script for the 0.12.0 upgrade, with the queue name filled in and any further
        /// placeholders replaced.
        /// </summary>
        /// <param name="fileName">File name inside docs/upgrade/0.12.0, e.g. "postgresql.sql".</param>
        /// <param name="queueName">The queue the script should target.</param>
        /// <param name="replacements">Further literal replacements, applied after the queue name.</param>
        public static string Read(string fileName, string queueName, IReadOnlyDictionary<string, string> replacements)
        {
            var path = Path.Combine(FolderForVersion("0.12.0"), fileName);
            if (!File.Exists(path))
                throw new FileNotFoundException($"No upgrade script at {path}", path);

            var script = File.ReadAllText(path).Replace(QueueNamePlaceholder, queueName);
            if (replacements == null)
                return script;

            foreach (var replacement in replacements)
                script = script.Replace(replacement.Key, replacement.Value);

            return script;
        }

        /// <summary>
        /// Walks up from the test binaries to the docs folder in the repository.
        /// </summary>
        /// <remarks>
        /// The scripts are not copied into the output: copying them would mean a stale copy can be
        /// tested after the real file changes, which is the whole failure this type exists to avoid.
        /// </remarks>
        private static string FolderForVersion(string version)
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null)
            {
                var candidate = Path.Combine(directory.FullName, "docs", "upgrade", version);
                if (Directory.Exists(candidate))
                    return candidate;

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException(
                $"No docs/upgrade/{version} folder above {AppContext.BaseDirectory}");
        }
    }
}
