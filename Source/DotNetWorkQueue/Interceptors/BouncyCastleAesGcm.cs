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
using System.Security.Cryptography;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace DotNetWorkQueue.Interceptors
{
    /// <summary>
    /// AES-GCM for the targets whose framework does not provide it.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>System.Security.Cryptography.AesGcm</c> arrived in .NET Core 3.0 and was never backported,
    /// so netstandard2.0 has no AES-GCM at all. Omitting the interceptor there was the alternative,
    /// and a worse one: a queue is shared between producers and consumers that need not be on the
    /// same framework, so a build that cannot read the others' messages is not much use.
    /// </para>
    /// <para>
    /// AES-GCM is fully specified by NIST SP 800-38D, so this produces the same bytes as the
    /// framework does for the same key, nonce, plaintext and associated data.
    /// <c>BouncyCastleAesGcmTests</c> asserts exactly that, comparing the two implementations
    /// directly - the test project compiles this file on net10.0 so the code netstandard2.0 runs is
    /// the code under test, rather than a restatement of it (GitHub #252).
    /// </para>
    /// <para>
    /// The file is compiled only into the netstandard2.0 build, so the dependency it needs is not
    /// imposed on consumers whose framework already has AES-GCM.
    /// </para>
    /// </remarks>
    internal static class BouncyCastleAesGcm
    {
        /// <summary>
        /// Encrypts <paramref name="plaintext"/>, writing the ciphertext and the authentication tag.
        /// </summary>
        internal static void Encrypt(byte[] key, byte[] nonce, byte[] plaintext, byte[] ciphertext,
            byte[] tag, byte[] associatedData)
        {
            var cipher = Create(true, key, nonce, tag.Length, associatedData);

            //BouncyCastle writes the tag immediately after the ciphertext rather than separately
            var output = new byte[cipher.GetOutputSize(plaintext.Length)];
            var written = cipher.ProcessBytes(plaintext, 0, plaintext.Length, output, 0);
            cipher.DoFinal(output, written);

            Buffer.BlockCopy(output, 0, ciphertext, 0, ciphertext.Length);
            Buffer.BlockCopy(output, ciphertext.Length, tag, 0, tag.Length);
        }

        /// <summary>
        /// Decrypts <paramref name="ciphertext"/> after checking <paramref name="tag"/>.
        /// </summary>
        /// <exception cref="CryptographicException">
        /// The tag does not match, so the message or its associated data was altered.
        /// </exception>
        internal static void Decrypt(byte[] key, byte[] nonce, byte[] ciphertext, byte[] tag,
            byte[] plaintext, byte[] associatedData)
        {
            var cipher = Create(false, key, nonce, tag.Length, associatedData);

            //and expects them back in that same order
            var input = new byte[ciphertext.Length + tag.Length];
            Buffer.BlockCopy(ciphertext, 0, input, 0, ciphertext.Length);
            Buffer.BlockCopy(tag, 0, input, ciphertext.Length, tag.Length);

            var output = new byte[cipher.GetOutputSize(input.Length)];
            int written;
            try
            {
                written = cipher.ProcessBytes(input, 0, input.Length, output, 0);
                written += cipher.DoFinal(output, written);
            }
            catch (InvalidCipherTextException error)
            {
                //the framework reports a failed tag as a CryptographicException, and callers catch
                //that. Letting a BouncyCastle type out here would make a message that is rejected
                //on one target throw something unrelated on the other.
                throw new CryptographicException("The computed authentication tag did not match the input authentication tag.", error);
            }

            Buffer.BlockCopy(output, 0, plaintext, 0, plaintext.Length);
        }

        /// <remarks>
        /// A cipher instance keeps state across a message, so each call gets its own rather than
        /// sharing one that two threads could interleave.
        /// </remarks>
        private static GcmBlockCipher Create(bool forEncryption, byte[] key, byte[] nonce,
            int tagSizeBytes, byte[] associatedData)
        {
            var cipher = new GcmBlockCipher(new AesEngine());
            cipher.Init(forEncryption,
                new AeadParameters(new KeyParameter(key), tagSizeBytes * 8, nonce, associatedData));
            return cipher;
        }
    }
}
