using System;
using System.Data.SQLite;
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
                // Use Path.Combine so the separator matches the OS — literal "\\" worked on
                // Windows but produces a non-separator character on Linux, breaking the
                // outbox-pattern ExternalTransactionValidator's path comparison (the caller
                // side's conn.DataSource canonicalizes one way, the queue side's parsed
                // Server value canonicalizes another).
                _fileName = Path.Combine(localPath, GenerateQueueName.CreateFileName());
                ConnectionString = $"Data Source={_fileName};Version=3;";

                using (var connection = new SQLiteConnection(ConnectionString))
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "PRAGMA journal_mode=WAL;";
                        command.ExecuteNonQuery();
                    }
                }
            }
        }
        public string ConnectionString
        {
            get;
        }

        [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "Not needed")]
        public void Dispose()
        {
            if (!string.IsNullOrWhiteSpace(_fileName))
            {
                try
                {
                    File.Delete(_fileName);
                }
                catch
                {
                    Thread.Sleep(3000);
                    try
                    {
                        File.Delete(_fileName);
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
        }
    }
}
