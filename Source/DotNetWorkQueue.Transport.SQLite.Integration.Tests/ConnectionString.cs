// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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

namespace DotNetWorkQueue.Transport.SQLite.Integration.Tests
{
    public class IntegrationConnectionInfo: IDisposable
    {
        private readonly string _fileName;

        public IntegrationConnectionInfo(bool inMemory)
        {
            if (inMemory)
            {
                //file:mymemorydb.db?mode=memory&cache=shared 
                ConnectionString = $"FullUri=file:{System.IO.Path.GetFileName(GenerateQueueName.CreateFileName())}?mode=memory&cache=shared;Version=3;";
            }
            else
            {
                //setup connection string
                string localPath = System.IO.Path.GetDirectoryName(new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);
                _fileName = localPath + "\\" + GenerateQueueName.CreateFileName();
                ConnectionString = $"Data Source={_fileName};Version=3;";
            }
        }
        public string ConnectionString
        {
            get;
            private set;
        }

        public void Dispose()
        {
            if (!string.IsNullOrWhiteSpace(_fileName))
            {
                try
                {
                    System.IO.File.Delete(_fileName);
                }
                catch
                {
                    System.Threading.Thread.Sleep(3000);
                    try
                    {
                        System.IO.File.Delete(_fileName);
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
