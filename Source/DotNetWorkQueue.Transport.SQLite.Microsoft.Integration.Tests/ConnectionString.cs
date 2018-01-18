using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading;
using DotNetWorkQueue.Transport.SQLite.Integration.Tests;

namespace DotNetWorkQueue.Transport.SQLite.Microsoft.Integration.Tests
{
    [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "Not needed")]
    public class IntegrationConnectionInfo: IDisposable
    {
        private readonly string _fileName;

        public IntegrationConnectionInfo(bool inMemory)
        {
            if (inMemory)
            {
                ConnectionString = $"Data Source=file:{Path.GetFileName(GenerateQueueName.CreateFileName())}?mode=memory&cache=shared;";
            }
            else
            {
                //setup connection string
                var localPath = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
                _fileName = localPath + "\\" + GenerateQueueName.CreateFileName();
                ConnectionString = $"Data Source={_fileName};";
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
