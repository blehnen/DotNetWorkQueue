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
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Interceptors
{
    /// <summary>
    /// Encrypts/decrypts a byte array using AES-256-GCM (authenticated encryption).
    /// Envelope: [version(1)=0x01][nonce(12)][tag(16)][ciphertext]. The version byte is
    /// authenticated as associated data, so it cannot be altered without failing the tag.
    /// </summary>
    /// <seealso cref="GZipMessageInterceptor"/>
    public class AesMessageInterceptor : IMessageInterceptor
    {
        private const byte Version = 0x01;
        private const int NonceSizeBytes = 12;
        private const int TagSizeBytes = 16;
        private const int HeaderSize = 1 + NonceSizeBytes + TagSizeBytes;

        private readonly AesMessageInterceptorConfiguration _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="AesMessageInterceptor" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public AesMessageInterceptor(AesMessageInterceptorConfiguration configuration)
        {
            Guard.NotNull(() => configuration, configuration);
            _configuration = configuration;
            DisplayName = "AES";
        }

        /// <summary>Encrypts the input byte array.</summary>
        /// <param name="input">The input.</param>
        /// <param name="headers">the message headers</param>
        public MessageInterceptorResult MessageToBytes(byte[] input, IReadOnlyDictionary<string, object> headers)
        {
            Guard.NotNull(() => input, input);

            var nonce = new byte[NonceSizeBytes];
            RandomNumberGenerator.Fill(nonce); // CSPRNG, not System.Random

            var tag = new byte[TagSizeBytes];
            var ciphertext = new byte[input.Length];
            var associatedData = new[] { Version };

            using (var aes = new AesGcm(_configuration.Key, TagSizeBytes))
            {
                aes.Encrypt(nonce, input, ciphertext, tag, associatedData);
            }

            var output = new byte[HeaderSize + ciphertext.Length];
            output[0] = Version;
            Buffer.BlockCopy(nonce, 0, output, 1, NonceSizeBytes);
            Buffer.BlockCopy(tag, 0, output, 1 + NonceSizeBytes, TagSizeBytes);
            Buffer.BlockCopy(ciphertext, 0, output, HeaderSize, ciphertext.Length);

            return new MessageInterceptorResult(output, true, GetType());
        }

        /// <summary>Decrypts the input byte array.</summary>
        /// <param name="input">The input.</param>
        /// <param name="headers">the message headers</param>
        public byte[] BytesToMessage(byte[] input, IReadOnlyDictionary<string, object> headers)
        {
            Guard.NotNull(() => input, input);
            if (input.Length < HeaderSize)
                throw new DotNetWorkQueueException("AES envelope is too short to contain the version, nonce, and tag.");
            if (input[0] != Version)
                throw new DotNetWorkQueueException($"Unsupported AES envelope version 0x{input[0]:X2}; expected 0x{Version:X2}.");

            var nonce = new byte[NonceSizeBytes];
            var tag = new byte[TagSizeBytes];
            var ciphertext = new byte[input.Length - HeaderSize];
            Buffer.BlockCopy(input, 1, nonce, 0, NonceSizeBytes);
            Buffer.BlockCopy(input, 1 + NonceSizeBytes, tag, 0, TagSizeBytes);
            Buffer.BlockCopy(input, HeaderSize, ciphertext, 0, ciphertext.Length);

            var plaintext = new byte[ciphertext.Length];
            var associatedData = new[] { Version };
            using (var aes = new AesGcm(_configuration.Key, TagSizeBytes))
            {
                aes.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);
            }
            return plaintext;
        }

        /// <summary>
        /// Gets the display name for logging or display purposes
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// The base type of the interceptor; used for re-creation
        /// </summary>
        public Type BaseType => GetType();
    }

    /// <summary>
    /// Configuration for <see cref="AesMessageInterceptor"/>. AES-256 requires a 32-byte key.
    /// The nonce is generated per message and is not part of this configuration.
    /// </summary>
    public class AesMessageInterceptorConfiguration
    {
        private const int KeySizeBytes = 32;

        /// <summary>
        /// Initializes a new instance of the <see cref="AesMessageInterceptorConfiguration"/> class.
        /// </summary>
        /// <param name="key">The AES-256 key; must be exactly 32 bytes.</param>
        public AesMessageInterceptorConfiguration(byte[] key)
        {
            Guard.NotNull(() => key, key);
            if (key.Length != KeySizeBytes)
                throw new ArgumentException($"AES-256 requires a {KeySizeBytes}-byte key; received {key.Length}.", nameof(key));
            Key = key;
        }

        /// <summary>
        /// Gets the AES-256 key (32 bytes).
        /// </summary>
        public byte[] Key { get; }
    }
}
