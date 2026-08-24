using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading;

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests
{
    [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "Not needed")]
    public class IntegrationConnectionInfo : IDisposable
    {
        private readonly string _fileName;
        private int _disposeCount;

        public IntegrationConnectionInfo(bool inMemory)
        {
            if (inMemory)
            {
                ConnectionString = $"FullUri=file:{Path.GetFileName(GenerateQueueName.CreateFileName())}?mode=memory&cache=shared;Version=3;";
            }
            else
            {
                //setup connection string
                var localPath = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
                _fileName = localPath + "\\" + GenerateQueueName.CreateFileName();
                ConnectionString = $"Data Source={_fileName};Version=3;";
            }
        }
        public string ConnectionString
        {
            get;
        }

        [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "Not needed")]
        public void Dispose()
        {
            //Dispose now has a process-wide side effect (clearing the connection pool), and these
            //fixtures are used from parallel test runs, so guard against running it twice.
            if (Interlocked.Increment(ref _disposeCount) != 1)
                return;

            if (!string.IsNullOrWhiteSpace(_fileName))
            {
                //The transport enables connection pooling by default, and a pooled connection keeps
                //the database file handle open. Without this, both deletes below fail and the
                //failure is swallowed, silently leaking a database file per test.
                SQLiteConnection.ClearAllPools();

                //WAL leaves -wal and -shm beside the database; deleting only the database leaks them.
                if (!TryDeleteWithSiblings())
                {
                    Thread.Sleep(3000);
                    if (!TryDeleteWithSiblings())
                    {
                        //Do not swallow this. A leaked file per test across the parallel CI stages
                        //fills the agent disk, and it is invisible if nothing reports it.
                        Console.WriteLine($"WARNING: could not delete test database '{_fileName}'. " +
                                          "A connection is still holding it open.");
                    }
                }
            }
        }

        private bool TryDeleteWithSiblings()
        {
            var deletedAll = true;
            foreach (var file in new[] { _fileName, _fileName + "-wal", _fileName + "-shm", _fileName + "-journal" })
            {
                //No File.Exists guard: Delete is a no-op for a file that is not there, and a
                //missing directory surfaces as DirectoryNotFoundException, which derives from
                //IOException and is handled below. Checking first would only add a syscall and a
                //window in which the answer could change.
                try
                {
                    File.Delete(file);
                }
                catch (IOException)
                {
                    deletedAll = false;
                }
                catch (UnauthorizedAccessException)
                {
                    deletedAll = false;
                }
            }
            return deletedAll;
        }
    }
}
