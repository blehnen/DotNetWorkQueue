using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotNetWorkQueue.Transport.Redis.Basic.Lua;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DotNetWorkQueue.Transport.Redis.Tests.Basic.Lua
{
    /// <summary>
    /// The batch enqueue script's MessagePack encoding (GitHub #416).
    /// </summary>
    /// <remarks>
    /// The expected bytes are what MsgPack.Cli wrote with <c>PackerCompatibilityOptions.Classic</c> before it was
    /// replaced, so a batch reaches <c>cmsgpack.unpack</c> exactly as it did.
    /// </remarks>
    [TestClass]
    public class LuaMessagePackTests
    {
        [TestMethod]
        [DataRow(0L, "00")]
        [DataRow(127L, "7F")]
        [DataRow(128L, "D10080")]
        [DataRow(255L, "D100FF")]
        [DataRow(256L, "D10100")]
        [DataRow(32767L, "D17FFF")]
        [DataRow(32768L, "D200008000")]
        [DataRow(65535L, "D20000FFFF")]
        [DataRow(2147483647L, "D27FFFFFFF")]
        [DataRow(4294967295L, "D300000000FFFFFFFF")]
        [DataRow(long.MaxValue, "D37FFFFFFFFFFFFFFF")]
        [DataRow(-1L, "FF")]
        [DataRow(-32L, "E0")]
        [DataRow(-33L, "D0DF")]
        [DataRow(-128L, "D080")]
        [DataRow(-129L, "D1FF7F")]
        [DataRow(-32768L, "D18000")]
        [DataRow(-32769L, "D2FFFF7FFF")]
        [DataRow(-2147483648L, "D280000000")]
        [DataRow(-2147483649L, "D3FFFFFFFF7FFFFFFF")]
        [DataRow(long.MinValue, "D38000000000000000")]
        public void An_Integer_Takes_The_Smallest_Signed_Form(long value, string expected)
        {
            Assert.AreEqual(expected, Hex(s => LuaMessagePack.WriteInt64(s, value)));
        }

        [TestMethod]
        [DataRow(0, "A0")]
        [DataRow(31, "BF")]
        [DataRow(32, "DA0020")]
        [DataRow(255, "DA00FF")]
        [DataRow(256, "DA0100")]
        [DataRow(65535, "DAFFFF")]
        [DataRow(65536, "DB00010000")]
        public void Raw_Bytes_Use_The_Old_Spec_Header(int length, string expectedHeader)
        {
            var value = Enumerable.Range(0, length).Select(i => (byte)i).ToArray();
            var packed = Pack(s => LuaMessagePack.WriteRaw(s, value));

            Assert.AreEqual(expectedHeader, Convert.ToHexString(packed, 0, expectedHeader.Length / 2));
            CollectionAssert.AreEqual(value, packed.Skip(expectedHeader.Length / 2).ToArray());
        }

        [TestMethod]
        public void A_String_Is_Utf8_With_The_Raw_Header()
        {
            Assert.AreEqual("A5C3A9E6BCA2", Hex(s => LuaMessagePack.WriteString(s, "é漢")));
            //a 32 byte string takes raw16; the current spec would write str8 (D9)
            Assert.AreEqual("DA0020", Hex(s => LuaMessagePack.WriteString(s, new string('x', 32))).Substring(0, 6));
        }

        [TestMethod]
        public void Null_Is_Nil()
        {
            Assert.AreEqual("C0", Hex(s => LuaMessagePack.WriteString(s, null)));
            Assert.AreEqual("C0", Hex(s => LuaMessagePack.WriteRaw(s, null)));
        }

        [TestMethod]
        [DataRow(0, "90")]
        [DataRow(15, "9F")]
        [DataRow(16, "DC0010")]
        [DataRow(65535, "DCFFFF")]
        [DataRow(65536, "DD00010000")]
        public void An_Array_Header_Takes_The_Smallest_Form(int count, string expected)
        {
            Assert.AreEqual(expected, Hex(s => LuaMessagePack.WriteArrayHeader(s, count)));
        }

        [TestMethod]
        public void A_Message_Packs_Its_Fields_In_The_Order_The_Script_Reads_Them()
        {
            //the script assigns positions 1 to 8: correlation id, expiration, headers, body, id, metadata, route, delay
            var message = new EnqueueBatchLua.MessageToSend
            {
                CorrelationId = "c",
                ExpireTimeStamp = 2,
                Headers = new byte[] { 3 },
                Message = new byte[] { 4 },
                MessageId = "5",
                MetaData = new byte[] { 6 },
                Route = "7",
                TimeStamp = 8
            };

            var packed = EnqueueBatchLua.Pack(new List<EnqueueBatchLua.MessageToSend> { message });

            Assert.AreEqual("91" + "98" + "A163" + "02" + "A103" + "A104" + "A135" + "A106" + "A137" + "08",
                Convert.ToHexString(packed));
        }

        [TestMethod]
        public void A_Missing_Route_Is_Packed_As_Empty()
        {
            //the script compares the route with '' to decide whether to index it
            var message = new EnqueueBatchLua.MessageToSend { Route = null, MessageId = string.Empty };

            var packed = EnqueueBatchLua.Pack(new List<EnqueueBatchLua.MessageToSend> { message });

            Assert.AreEqual("9198C000C0C0A0C0A000", Convert.ToHexString(packed));
        }

        [TestMethod]
        public void Every_String_Encoding_Is_Read()
        {
            var str8 = new string('a', 40);
            var str16 = new string('b', 300);
            var str32 = new string('c', 70000);
            var data = Concat(
                "97", //array of 7
                "A5C3A9E6BCA2",
                "D9" + str8.Length.ToString("X2"), Ascii(str8),
                "DA" + str16.Length.ToString("X4"), Ascii(str16),
                "DB" + str32.Length.ToString("X8"), Ascii(str32),
                "C403", Ascii("bin"),
                "C0",
                "A0");

            var result = LuaMessagePack.ReadStringArray(data);

            CollectionAssert.AreEqual(new[] { "é漢", str8, str16, str32, "bin", null, string.Empty }, result);
        }

        [TestMethod]
        public void A_Long_Array_Is_Read()
        {
            var data = Concat("DC0010", string.Concat(Enumerable.Repeat("A131", 16)));

            CollectionAssert.AreEqual(Enumerable.Repeat("1", 16).ToList(), LuaMessagePack.ReadStringArray(data));
        }

        [TestMethod]
        [DataRow("90")]
        [DataRow("80")] //cmsgpack's form for an empty Lua table
        public void An_Empty_Table_Is_An_Empty_List(string hex)
        {
            Assert.IsEmpty(LuaMessagePack.ReadStringArray(Convert.FromHexString(hex)));
        }

        [TestMethod]
        [DataRow("", DisplayName = "no data")]
        [DataRow("A161", DisplayName = "not an array")]
        [DataRow("9101", DisplayName = "an integer element")]
        [DataRow("92A161", DisplayName = "fewer elements than announced")]
        [DataRow("91A361", DisplayName = "a string shorter than announced")]
        [DataRow("91DA00", DisplayName = "a truncated length")]
        [DataRow("91A161A162", DisplayName = "bytes after the array")]
        [DataRow("91DBFFFFFFFF", DisplayName = "a length past int.MaxValue")]
        public void Malformed_Data_Is_Rejected(string hex)
        {
            Assert.ThrowsExactly<InvalidDataException>(() => LuaMessagePack.ReadStringArray(Convert.FromHexString(hex)));
        }

        private static byte[] Pack(Action<Stream> write)
        {
            using var stream = new MemoryStream();
            write(stream);
            return stream.ToArray();
        }

        private static string Hex(Action<Stream> write) => Convert.ToHexString(Pack(write));

        private static string Ascii(string value) => Convert.ToHexString(System.Text.Encoding.ASCII.GetBytes(value));

        private static byte[] Concat(params string[] hex) => Convert.FromHexString(string.Concat(hex));
    }
}
