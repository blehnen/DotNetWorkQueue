// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2017 Brian Lehnen
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
namespace DotNetWorkQueue.Transport.Redis.IntegrationTests
{
    public class ConnectionInfo
    {
        /// <summary>
        /// The connection string to the redis server for the integration tests. All tests in this project will use this connection string for linux tests
        /// </summary>
        private const string ConnectionStringLinux = "192.168.0.212,defaultDatabase=1,syncTimeout=15000";
        /// <summary>
        /// The connection string to the redis server for the integration tests. All tests in this project will use this connection string for windows tests
        /// </summary>
        private const string ConnectionStringWindows = "V-WINRedis,defaultDatabase=1,syncTimeout=15000";
        private readonly ConnectionInfoTypes _type;
        public ConnectionInfo(ConnectionInfoTypes type)
        {
            _type = type;
        }

        public string ConnectionString
        {
            get
            {
                switch (_type)
                {
                    case ConnectionInfoTypes.Linux:
                        return ConnectionStringLinux;
                    case ConnectionInfoTypes.Windows:
                        return ConnectionStringWindows;
                    default:
                        return string.Empty;
                }
            }
        }
    }

    public enum ConnectionInfoTypes
    {
        Linux = 0,
        Windows = 1
    }
}
