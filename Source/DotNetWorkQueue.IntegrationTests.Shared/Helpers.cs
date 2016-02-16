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
using System.Text;
using Newtonsoft.Json;
namespace DotNetWorkQueue.IntegrationTests.Shared
{
    /// <summary>
    /// Allows easier re-usage of a byref counter
    /// </summary>
    public class IncrementWrapper
    {
        public IncrementWrapper()
        {
            ProcessedCount = 0;
        }
        public long ProcessedCount;
    }

    public class SerializerThatWillCrashOnDeSerialization : ISerializer
    {
        private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };

        public byte[] ConvertMessageToBytes<T>(T message) where T : class
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(message, _serializerSettings));
        }

        public T ConvertBytesToMessage<T>(byte[] bytes) where T : class
        {
            // ReSharper disable once UnthrowableException
            throw new AccessViolationException("Permission denied");
        }
    }
}
