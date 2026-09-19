using System.Text;

namespace DotNetWorkQueue.IntegrationTests.Shared
{
    /// <summary>
    /// The AES key the integration suites encrypt with.
    /// </summary>
    /// <remarks>
    /// One place rather than the nine copies of the same literal this replaces. It is a test key
    /// and deliberately obvious: nothing here needs an unpredictable one, and a reader should not
    /// have to wonder whether it was meant to be a real secret. AES-256 wants 32 bytes, which is
    /// what these 32 ASCII characters give.
    /// </remarks>
    public static class TestEncryptionKey
    {
        /// <summary>The 32 byte AES-256 key.</summary>
        public static byte[] Aes256 => Encoding.ASCII.GetBytes("0123456789abcdef0123456789abcdef");
    }
}
