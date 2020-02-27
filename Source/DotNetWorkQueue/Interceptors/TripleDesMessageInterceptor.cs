// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2020 Brian Lehnen
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
using System.Collections.Generic;
using System.Security.Cryptography;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Interceptors
{
    /// <summary>
    /// Encrypts/Decrypts a byte array
    /// </summary>
    public class TripleDesMessageInterceptor : IMessageInterceptor
    {
        #region Member level variables
        private readonly TripleDesMessageInterceptorConfiguration _configuration;
        #endregion

        #region Constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="TripleDesMessageInterceptor" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public TripleDesMessageInterceptor(TripleDesMessageInterceptorConfiguration configuration)
        {
            _configuration = configuration;
            DisplayName = "TripleDes";
        }
        #endregion

        #region Encryption
        /// <summary>Encrypts the input byte array</summary>
        /// <param name="input">The input.</param>
        /// <param name="headers">the message headers</param>
        /// <returns></returns>
        public MessageInterceptorResult MessageToBytes(byte[] input, IReadOnlyDictionary<string, object> headers)
        {
            Guard.NotNull(() => input, input);
            using (var tripleDes = TripleDES.Create())
            {
                using (var tripleDesEncrypt = tripleDes.CreateEncryptor(_configuration.Key, _configuration.Iv))
                {
                    return new MessageInterceptorResult(tripleDesEncrypt.TransformFinalBlock(input, 0, input.Length), true, GetType());
                }
            }
        }

        /// <summary>
        /// Decrypts the input byte array
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="headers">the message headers</param>
        /// <returns></returns>
        public byte[] BytesToMessage(byte[] input, IReadOnlyDictionary<string, object> headers)
        {
            Guard.NotNull(() => input, input);
            using (var tripleDes = TripleDES.Create())
            {
                using (var tripleDesDecrypt = tripleDes.CreateDecryptor(_configuration.Key, _configuration.Iv))
                {
                    return tripleDesDecrypt.TransformFinalBlock(input, 0, input.Length);
                }
            }
        }
        /// <summary>
        /// Gets the display name for logging or display purposes
        /// </summary>
        /// <value>
        /// The display name.
        /// </value>
        public string DisplayName { get; }

        /// <summary>
        /// The base type of the interceptor; used for re-creation
        /// </summary>
        /// <value>
        /// The type of the base.
        /// </value>
        public Type BaseType => GetType();

        #endregion
    }

    /// <summary>
    /// Configuration class for <see cref="TripleDesMessageInterceptor"/>
    /// </summary>
    public class TripleDesMessageInterceptorConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TripleDesMessageInterceptorConfiguration"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="iv">The initialization vector.</param>
        public TripleDesMessageInterceptorConfiguration(byte[] key, byte[] iv)
        {
            Key = key;
            Iv = iv;
        }

        /// <summary>
        /// Gets the key.
        /// </summary>
        /// <value>
        /// The key.
        /// </value>
        public byte[] Key { get; }
        /// <summary>
        /// Gets the initialization vector.
        /// </summary>
        /// <value>
        /// The initialization vector.
        /// </value>
        public byte[] Iv { get; }
    }
}
