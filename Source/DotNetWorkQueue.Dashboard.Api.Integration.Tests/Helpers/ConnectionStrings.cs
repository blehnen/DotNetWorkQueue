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
using System.IO;

namespace DotNetWorkQueue.Dashboard.Api.Integration.Tests.Helpers
{
    public static class ConnectionStrings
    {
        public static string SqlServer
        {
            get
            {
                var path = Path.Combine(Directory.GetCurrentDirectory(), "connectionstring.txt");
                return File.ReadAllText(path).Trim();
            }
        }

        public static string PostgreSql =>
            "Server=192.168.0.2;Port=5432;Database=integrationtesting;Maximum Pool Size=250;userid=brian;Trust Server Certificate=true;Keepalive=15;Tcp Keepalive=true;";

        public static string Redis => "192.168.0.2,defaultDatabase=1,syncTimeout=15000";

        public static string CreateSqliteInMemory(string queueName) =>
            $"FullUri=file:{queueName}?mode=memory&cache=shared;Version=3;";

        public static string LiteDbMemory => ":memory:";

        public static string Memory => "none";
    }
}
