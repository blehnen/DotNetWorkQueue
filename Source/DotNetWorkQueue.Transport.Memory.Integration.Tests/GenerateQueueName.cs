#region Using

using System;
using System.Security.Cryptography;
using System.Text;

#endregion

namespace DotNetWorkQueue.Transport.Memory.Integration.Tests
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
            return "I" + BitConverter.ToString(hash)
               .Replace("-", string.Empty)
               .Replace("_", string.Empty)
               .ToLower();

        }
        public static string CreateFileName()
        {
            return Create() + ".db3";
        }
    }
}
