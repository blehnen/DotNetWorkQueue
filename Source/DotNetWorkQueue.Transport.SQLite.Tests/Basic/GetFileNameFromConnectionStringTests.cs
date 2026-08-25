using System;
using System.IO;
using DotNetWorkQueue.Transport.SQLite.Basic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.SQLite.Tests.Basic
{
    /// <summary>
    /// Covers <see cref="GetFileNameFromConnectionString"/>, whose result is cached because parsing
    /// a connection string is the single largest allocation on the send path.
    /// </summary>
    [TestClass]
    public class GetFileNameFromConnectionStringTests
    {
        private static readonly GetFileNameFromConnectionString Parser = new GetFileNameFromConnectionString();

        [TestMethod]
        public void FileDatabase_ReturnsTheFileName()
        {
            var result = Parser.GetFileName(@"Data Source=c:\test\temp.db3;Version=3;");

            Assert.IsFalse(result.IsInMemory);
            Assert.AreEqual(@"c:\test\temp.db3", result.FileName);
            Assert.IsTrue(result.IsValid);
        }

        [TestMethod]
        public void InMemoryDatabase_IsFlagged()
        {
            var result = Parser.GetFileName("Data Source=:memory:;Version=3;");

            Assert.IsTrue(result.IsInMemory);
        }

        [TestMethod]
        public void SharedCacheInMemoryDatabase_IsFlagged()
        {
            //the form the integration tests use; the marker is in the full string, not the data source
            var result = Parser.GetFileName("FullUri=file:test.db3?mode=memory&cache=shared;Version=3;");

            Assert.IsTrue(result.IsInMemory);
        }

        [TestMethod]
        public void MalformedConnectionString_IsNotValid()
        {
            var result = Parser.GetFileName("Thisisabadconnectionstring");

            Assert.IsFalse(result.IsInMemory);
            Assert.IsFalse(result.IsValid);
        }

        [TestMethod]
        public void NullOrEmpty_IsNotValid()
        {
            //neither can be a cache key, so both take the uncached path
            Assert.IsFalse(Parser.GetFileName(null).IsValid);
            Assert.IsFalse(Parser.GetFileName(string.Empty).IsValid);
        }

        [TestMethod]
        public void RepeatedCalls_ReturnTheSameAnswer()
        {
            const string connectionString = @"Data Source=c:\test\repeat.db3;Version=3;";

            var first = Parser.GetFileName(connectionString);
            var second = Parser.GetFileName(connectionString);

            Assert.AreEqual(first.FileName, second.FileName);
            Assert.AreEqual(first.IsInMemory, second.IsInMemory);
        }

        [TestMethod]
        public void DifferentConnectionStrings_DoNotShareAnAnswer()
        {
            //a regression guard for the cache: a second string must not receive the first's result
            var first = Parser.GetFileName(@"Data Source=c:\test\one.db3;Version=3;");
            var second = Parser.GetFileName(@"Data Source=c:\test\two.db3;Version=3;");

            Assert.AreEqual(@"c:\test\one.db3", first.FileName);
            Assert.AreEqual(@"c:\test\two.db3", second.FileName);
        }

        [TestMethod]
        public void CasingOfTheConnectionString_IsNotTreatedAsTheSameEntry()
        {
            //the cache compares ordinally, so these are two entries holding the same answer rather
            //than one entry serving both - either way the answer must be right
            var lower = Parser.GetFileName(@"data source=c:\test\case.db3;version=3;");
            var upper = Parser.GetFileName(@"DATA SOURCE=c:\test\case.db3;VERSION=3;");

            Assert.AreEqual(@"c:\test\case.db3", lower.FileName);
            Assert.AreEqual(@"c:\test\case.db3", upper.FileName);
        }

        [TestMethod]
        public void StaysCorrectBeyondTheCacheCap()
        {
            //past the cap the answer is parsed each time rather than cached; it must still be right
            for (var i = 0; i < 300; i++)
            {
                var connectionString = $@"Data Source=c:\test\cap{i}.db3;Version=3;";

                Assert.AreEqual($@"c:\test\cap{i}.db3", Parser.GetFileName(connectionString).FileName);
            }
        }

        [TestMethod]
        public void AnExistingFile_IsFoundThroughDatabaseExists()
        {
            //the cached parse feeds DatabaseExists, which still checks the file system every call
            var fileName = Path.GetTempFileName();
            var connectionString = $"Data Source={fileName};Version=3;";
            var exists = new DatabaseExists(Parser);
            try
            {
                Assert.IsTrue(exists.Exists(connectionString));

                File.Delete(fileName);

                Assert.IsFalse(exists.Exists(connectionString),
                    "a deleted database must not be reported as existing because its parse was cached");
            }
            finally
            {
                if (File.Exists(fileName))
                    File.Delete(fileName);
            }
        }
    }
}
