using System;
using System.Security.Cryptography;
using System.Text;
using DotNetWorkQueue.Exceptions;
using DotNetWorkQueue.Interceptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Tests.Interceptors
{
    [TestClass]
    public class AesMessageInterceptorTests
    {
        private static byte[] NewKey() { var k = new byte[32]; RandomNumberGenerator.Fill(k); return k; }
        private static AesMessageInterceptor New(byte[] key) => new(new AesMessageInterceptorConfiguration(key));

        [TestMethod]
        public void Config_RejectsNon32ByteKey()
        {
            Assert.ThrowsExactly<ArgumentException>(() => new AesMessageInterceptorConfiguration(new byte[16]));
        }

        [TestMethod]
        public void RoundTrip_ReturnsOriginal()
        {
            var key = NewKey();
            var interceptor = New(key);
            foreach (var size in new[] { 0, 1, 150, 100_000 })
            {
                var original = new byte[size];
                RandomNumberGenerator.Fill(original);
                var encrypted = interceptor.MessageToBytes(original, null);
                Assert.IsTrue(encrypted.AddToGraph);
                var decrypted = New(key).BytesToMessage(encrypted.Output, null);
                CollectionAssert.AreEqual(original, decrypted);
            }
        }

        [TestMethod]
        public void Envelope_HasVersionNonceTagHeader()
        {
            var enc = New(NewKey()).MessageToBytes(Encoding.UTF8.GetBytes("hello"), null);
            Assert.AreEqual((byte)0x01, enc.Output[0]);
            Assert.AreEqual(1 + 12 + 16 + "hello"u8.Length, enc.Output.Length);
        }

        [TestMethod]
        public void SamePlaintext_ProducesDifferentCiphertext()
        {
            var interceptor = New(NewKey());
            var body = Encoding.UTF8.GetBytes("same message");
            var a = interceptor.MessageToBytes(body, null).Output;
            var b = interceptor.MessageToBytes(body, null).Output;
            CollectionAssert.AreNotEqual(a, b); // random nonce
        }

        [TestMethod]
        public void TamperedCiphertext_Throws()
        {
            var key = NewKey();
            var enc = New(key).MessageToBytes(Encoding.UTF8.GetBytes("secret"), null).Output;
            enc[^1] ^= 0xFF; // flip last ciphertext byte
            // AesGcm throws AuthenticationTagMismatchException : CryptographicException -> use Throws<> (T-or-derived)
            Assert.Throws<CryptographicException>(() => New(key).BytesToMessage(enc, null));
        }

        [TestMethod]
        public void TamperedVersionByte_Throws()
        {
            var key = NewKey();
            var enc = New(key).MessageToBytes(Encoding.UTF8.GetBytes("secret"), null).Output;
            enc[0] = 0x02; // unknown version -> rejected before decrypt
            Assert.ThrowsExactly<DotNetWorkQueueException>(() => New(key).BytesToMessage(enc, null));
        }

        [TestMethod]
        public void WrongKey_Throws()
        {
            var enc = New(NewKey()).MessageToBytes(Encoding.UTF8.GetBytes("secret"), null).Output;
            Assert.Throws<CryptographicException>(() => New(NewKey()).BytesToMessage(enc, null));
        }

        [TestMethod]
        public void ShortInput_Throws()
        {
            Assert.ThrowsExactly<DotNetWorkQueueException>(() => New(NewKey()).BytesToMessage(new byte[5], null));
        }
    }
}
