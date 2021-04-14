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
        private string _fileName;

        public IntegrationConnectionInfo(ConnectionTypes connectionType)
        {
            SetConnection(connectionType);
        }

        private void SetConnection(ConnectionTypes connectionType)
        {
            switch (connectionType)
            {
                case ConnectionTypes.Memory:
                    ConnectionString = ":memory:";
                    break;
                case ConnectionTypes.Direct:
                case ConnectionTypes.Shared:
                    var localPath = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
                    _fileName = localPath + "\\" + GenerateQueueName.CreateFileName();
                    ConnectionString = connectionType == ConnectionTypes.Direct ? $"Filename={_fileName};Connection=direct;" : $"Filename={_fileName};Connection=shared;";
                    break;
            }
        }

        public string ConnectionString
        {
            get;
            private set;
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

        public enum ConnectionTypes
        {
            Shared = 0,
            Direct = 1, 
            Memory = 2
        }
    }
}
