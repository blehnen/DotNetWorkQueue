using System;
using System.Security.Cryptography;
using DotNetWorkQueue.Interceptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Interceptors
{
    /// <summary>
    /// <see cref="BouncyCastleAesGcm"/> is the AES-GCM the netstandard2.0 build uses, because that
    /// framework has none of its own.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The property that matters is not "does it encrypt" but "does it produce what the other
    /// builds produce". A queue is shared, and a producer on .NET 10 and a consumer on .NET
    /// Framework have to agree on every byte or the message is unreadable. So these compare the two
    /// implementations directly rather than round-tripping each on its own, which would pass
    /// happily while the two disagreed.
    /// </para>
    /// <para>
    /// The test project compiles the shipping file, so this covers the code netstandard2.0 runs
    /// even though the suite itself runs on net10.0 (GitHub #252).
    /// </para>
    /// </remarks>
    [TestClass]
    public class BouncyCastleAesGcmTests
    {
        private const int NonceSizeBytes = 12;
        private const int TagSizeBytes = 16;

        //the byte AesMessageInterceptor authenticates as associated data
        private static readonly byte[] AssociatedData = { 0x01 };

        [TestMethod]
        public void It_Produces_The_Same_Bytes_As_The_Framework()
        {
            var (key, nonce) = NewKeyAndNonce();
            var plaintext = Plaintext(117);

            var (theirs, theirTag) = FrameworkEncrypt(key, nonce, plaintext);
            var (ours, ourTag) = Encrypt(key, nonce, plaintext);

            Assert.AreEqual(Convert.ToBase64String(theirs), Convert.ToBase64String(ours),
                "a message encrypted on netstandard2.0 would not match one encrypted anywhere else");
            Assert.AreEqual(Convert.ToBase64String(theirTag), Convert.ToBase64String(ourTag),
                "the authentication tags differ, so neither build could read the other's messages");
        }

        [TestMethod]
        public void It_Agrees_With_The_Framework_At_Every_Length()
        {
            //GCM works in 16 byte blocks, so the lengths worth checking are the empty message, one
            //under a block, exactly a block, one over, and several blocks with a partial one left
            var (key, nonce) = NewKeyAndNonce();

            foreach (var length in new[] { 0, 1, 15, 16, 17, 31, 32, 33, 1024, 4097 })
            {
                var plaintext = Plaintext(length);
                var (theirs, theirTag) = FrameworkEncrypt(key, nonce, plaintext);
                var (ours, ourTag) = Encrypt(key, nonce, plaintext);

                Assert.AreEqual(Convert.ToBase64String(theirs), Convert.ToBase64String(ours),
                    $"ciphertexts differ at {length} bytes");
                Assert.AreEqual(Convert.ToBase64String(theirTag), Convert.ToBase64String(ourTag),
                    $"tags differ at {length} bytes");
            }
        }

        [TestMethod]
        public void It_Reads_What_The_Framework_Wrote()
        {
            var (key, nonce) = NewKeyAndNonce();
            var plaintext = Plaintext(200);
            var (ciphertext, tag) = FrameworkEncrypt(key, nonce, plaintext);

            var read = new byte[ciphertext.Length];
            BouncyCastleAesGcm.Decrypt(key, nonce, ciphertext, tag, read, AssociatedData);

            Assert.AreEqual(Convert.ToBase64String(plaintext), Convert.ToBase64String(read));
        }

        [TestMethod]
        public void The_Framework_Reads_What_It_Wrote()
        {
            var (key, nonce) = NewKeyAndNonce();
            var plaintext = Plaintext(200);
            var (ciphertext, tag) = Encrypt(key, nonce, plaintext);

            var read = new byte[ciphertext.Length];
            using var aes = new AesGcm(key, TagSizeBytes);
            aes.Decrypt(nonce, ciphertext, tag, read, AssociatedData);

            Assert.AreEqual(Convert.ToBase64String(plaintext), Convert.ToBase64String(read));
        }

        [TestMethod]
        public void A_Tampered_Message_Is_Refused_As_A_CryptographicException()
        {
            //the type matters as much as the refusal: AesMessageInterceptor's callers catch
            //CryptographicException, and a BouncyCastle type reaching them would be unhandled
            var (key, nonce) = NewKeyAndNonce();
            var (ciphertext, tag) = Encrypt(key, nonce, Plaintext(64));
            ciphertext[0] ^= 0xFF;

            Assert.Throws<CryptographicException>(() =>
                BouncyCastleAesGcm.Decrypt(key, nonce, ciphertext, tag, new byte[ciphertext.Length], AssociatedData));
        }

        [TestMethod]
        public void A_Tampered_Tag_Is_Refused()
        {
            var (key, nonce) = NewKeyAndNonce();
            var (ciphertext, tag) = Encrypt(key, nonce, Plaintext(64));
            tag[0] ^= 0xFF;

            Assert.Throws<CryptographicException>(() =>
                BouncyCastleAesGcm.Decrypt(key, nonce, ciphertext, tag, new byte[ciphertext.Length], AssociatedData));
        }

        [TestMethod]
        public void A_Changed_Version_Byte_Is_Refused()
        {
            //the envelope's version is associated data rather than plaintext, so altering it has to
            //fail the tag instead of silently decrypting under the wrong version
            var (key, nonce) = NewKeyAndNonce();
            var (ciphertext, tag) = Encrypt(key, nonce, Plaintext(64));

            Assert.Throws<CryptographicException>(() =>
                BouncyCastleAesGcm.Decrypt(key, nonce, ciphertext, tag, new byte[ciphertext.Length], new byte[] { 0x02 }));
        }

        [TestMethod]
        public void The_Wrong_Key_Is_Refused()
        {
            var (key, nonce) = NewKeyAndNonce();
            var (ciphertext, tag) = Encrypt(key, nonce, Plaintext(64));
            var (other, _) = NewKeyAndNonce();

            Assert.Throws<CryptographicException>(() =>
                BouncyCastleAesGcm.Decrypt(other, nonce, ciphertext, tag, new byte[ciphertext.Length], AssociatedData));
        }

        private static (byte[] Ciphertext, byte[] Tag) Encrypt(byte[] key, byte[] nonce, byte[] plaintext)
        {
            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[TagSizeBytes];
            BouncyCastleAesGcm.Encrypt(key, nonce, plaintext, ciphertext, tag, AssociatedData);
            return (ciphertext, tag);
        }

        private static (byte[] Ciphertext, byte[] Tag) FrameworkEncrypt(byte[] key, byte[] nonce, byte[] plaintext)
        {
            var ciphertext = new byte[plaintext.Length];
            var tag = new byte[TagSizeBytes];
            using var aes = new AesGcm(key, TagSizeBytes);
            aes.Encrypt(nonce, plaintext, ciphertext, tag, AssociatedData);
            return (ciphertext, tag);
        }

        private static (byte[] Key, byte[] Nonce) NewKeyAndNonce()
        {
            var key = new byte[32];
            var nonce = new byte[NonceSizeBytes];
            RandomNumberGenerator.Fill(key);
            RandomNumberGenerator.Fill(nonce);
            return (key, nonce);
        }

        /// <summary>A repeatable body, so a failure names a length rather than a random blob.</summary>
        private static byte[] Plaintext(int length)
        {
            var value = new byte[length];
            for (var i = 0; i < length; i++)
                value[i] = (byte)(i % 251);
            return value;
        }
    }
}
