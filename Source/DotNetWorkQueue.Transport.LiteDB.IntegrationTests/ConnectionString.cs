using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading;

namespace DotNetWorkQueue.Transport.LiteDb.IntegrationTests
{
    [SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "Not needed")]
    public class IntegrationConnectionInfo: IDisposable
    {
        private readonly string _fileName;

        public IntegrationConnectionInfo()
        {
            //setup connection string
            var localPath = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            _fileName = localPath + "\\" + GenerateQueueName.CreateFileName();
            ConnectionString = $"Filename={_fileName};Connection=shared;";
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
