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
using System.Security.Cryptography;

namespace DotNetWorkQueue.Interceptors
{
    /// <summary>
    /// AES-GCM, from whichever implementation the target framework can offer.
    /// </summary>
    /// <remarks>
    /// The envelope - its layout, its version byte, its sizes - belongs to
    /// <see cref="AesMessageInterceptor"/> and is the same everywhere. Only the primitive differs,
    /// and only because <c>AesGcm</c> arrived in .NET Core 3.0 and was never backported to
    /// netstandard2.0. Both produce the same bytes for the same inputs, so a message written by one
    /// build is readable by the other (GitHub #252).
    /// </remarks>
    internal static class AesGcmCipher
    {
        /// <summary>
        /// Encrypts <paramref name="plaintext"/>, writing the ciphertext and the authentication tag.
        /// </summary>
        internal static void Encrypt(byte[] key, byte[] nonce, byte[] plaintext, byte[] ciphertext,
            byte[] tag, byte[] associatedData)
        {
#if NETSTANDARD2_0
            BouncyCastleAesGcm.Encrypt(key, nonce, plaintext, ciphertext, tag, associatedData);
#else
            using var aes = new AesGcm(key, tag.Length);
            aes.Encrypt(nonce, plaintext, ciphertext, tag, associatedData);
#endif
        }

        /// <summary>
        /// Decrypts <paramref name="ciphertext"/> after checking <paramref name="tag"/>.
        /// </summary>
        /// <exception cref="CryptographicException">
        /// The tag does not match, so the message or its associated data was altered. Both
        /// implementations report it as this type.
        /// </exception>
        internal static void Decrypt(byte[] key, byte[] nonce, byte[] ciphertext, byte[] tag,
            byte[] plaintext, byte[] associatedData)
        {
#if NETSTANDARD2_0
            BouncyCastleAesGcm.Decrypt(key, nonce, ciphertext, tag, plaintext, associatedData);
#else
            using var aes = new AesGcm(key, tag.Length);
            aes.Decrypt(nonce, ciphertext, tag, plaintext, associatedData);
#endif
        }
    }
}
