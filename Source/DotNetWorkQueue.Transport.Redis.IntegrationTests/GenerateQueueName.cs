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

using System;
using System.Security.Cryptography;
using System.Text;

namespace DotNetWorkQueue.Transport.Redis.IntegrationTests
{
    public static class GenerateQueueName
    {
        /// <summary>
        /// Tried a datetime, but we can start the tests too fast - ended up with duplicates every once and a while.
        /// </summary>
        /// <returns></returns>
        public static string Create()
        {
            var encoded = new UTF8Encoding().GetBytes(Guid.NewGuid().ToString());
            var hash = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")).ComputeHash(encoded);
            return "IT" + BitConverter.ToString(hash)
               .Replace("-", string.Empty)
               .Replace("_", string.Empty)
               .ToLower();
        }
    }
}
