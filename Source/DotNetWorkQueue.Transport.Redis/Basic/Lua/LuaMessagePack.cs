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
using System.Collections.Generic;
using System.IO;
using System.Text;
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.Transport.Redis.Basic.Lua
{
    /// <summary>
    /// The subset of MessagePack that the batch enqueue script exchanges with Redis's <c>cmsgpack</c>.
    /// </summary>
    /// <remarks>
    /// Writes the old (raw) spec: strings and byte arrays share the raw header, and there is no str8 or bin
    /// type. Integers use a positive or negative fixint where one fits, otherwise the smallest signed type.
    /// These are the bytes MsgPack.Cli wrote with <c>PackerCompatibilityOptions.Classic</c>, which this replaces.
    /// Reading accepts every string, bin and nil encoding, because <c>cmsgpack.pack</c> writes the current spec.
    /// </remarks>
    internal static class LuaMessagePack
    {
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false);

        /// <summary>Writes an array header for <paramref name="count"/> elements.</summary>
        public static void WriteArrayHeader(Stream output, int count)
        {
            if (count < 16)
                output.WriteByte((byte)(0x90 | count));
            else if (count <= ushort.MaxValue)
            {
                output.WriteByte(0xdc);
                WriteBigEndian(output, (ulong)count, 2);
            }
            else
            {
                output.WriteByte(0xdd);
                WriteBigEndian(output, (ulong)count, 4);
            }
        }

        /// <summary>Writes a string as UTF-8 raw bytes; null is written as nil.</summary>
        public static void WriteString(Stream output, string value)
        {
            WriteRaw(output, value == null ? null : Utf8.GetBytes(value));
        }

        /// <summary>Writes a byte array as raw bytes; null is written as nil.</summary>
        public static void WriteRaw(Stream output, byte[] value)
        {
            if (value == null)
            {
                output.WriteByte(0xc0);
                return;
            }

            if (value.Length < 32)
                output.WriteByte((byte)(0xa0 | value.Length));
            else if (value.Length <= ushort.MaxValue)
            {
                output.WriteByte(0xda);
                WriteBigEndian(output, (ulong)value.Length, 2);
            }
            else
            {
                output.WriteByte(0xdb);
                WriteBigEndian(output, (ulong)value.Length, 4);
            }
            output.Write(value, 0, value.Length);
        }

        /// <summary>Writes an integer in the smallest form that holds it.</summary>
        public static void WriteInt64(Stream output, long value)
        {
            if (value >= -32 && value <= 127)
                output.WriteByte(unchecked((byte)value)); //positive or negative fixint
            else if (value >= sbyte.MinValue && value <= sbyte.MaxValue)
            {
                output.WriteByte(0xd0);
                output.WriteByte(unchecked((byte)value));
            }
            else if (value >= short.MinValue && value <= short.MaxValue)
            {
                output.WriteByte(0xd1);
                WriteBigEndian(output, unchecked((ulong)value), 2);
            }
            else if (value >= int.MinValue && value <= int.MaxValue)
            {
                output.WriteByte(0xd2);
                WriteBigEndian(output, unchecked((ulong)value), 4);
            }
            else
            {
                output.WriteByte(0xd3);
                WriteBigEndian(output, unchecked((ulong)value), 8);
            }
        }

        /// <summary>
        /// Reads an array of strings, as <c>cmsgpack.pack</c> writes a Lua table of strings. A nil element is read as null.
        /// </summary>
        /// <exception cref="InvalidDataException">The data is not an array of strings, or is truncated.</exception>
        public static List<string> ReadStringArray(byte[] data)
        {
            Guard.NotNull(data);
            var position = 0;
            var count = ReadCollectionHeader(data, ref position);
            var result = new List<string>(count);
            for (var i = 0; i < count; i++)
                result.Add(ReadString(data, ref position));
            if (position != data.Length)
                throw new InvalidDataException($"{data.Length - position} unread byte(s) follow the array");
            return result;
        }

        private static int ReadCollectionHeader(byte[] data, ref int position)
        {
            var code = ReadByte(data, ref position);
            if (code >= 0x90 && code <= 0x9f)
                return code & 0x0f;
            switch (code)
            {
                case 0xdc:
                    return (int)ReadBigEndian(data, ref position, 2);
                case 0xdd:
                    return ToLength(ReadBigEndian(data, ref position, 4));
                //cmsgpack packs an empty Lua table as an empty map
                case 0x80:
                    return 0;
                default:
                    throw new InvalidDataException($"Expected an array, found type code 0x{code:x2}");
            }
        }

        private static string ReadString(byte[] data, ref int position)
        {
            var code = ReadByte(data, ref position);
            int length;
            if (code >= 0xa0 && code <= 0xbf)
                length = code & 0x1f;
            else
            {
                switch (code)
                {
                    case 0xc0:
                        return null;
                    case 0xd9:
                    case 0xc4:
                        length = (int)ReadBigEndian(data, ref position, 1);
                        break;
                    case 0xda:
                    case 0xc5:
                        length = (int)ReadBigEndian(data, ref position, 2);
                        break;
                    case 0xdb:
                    case 0xc6:
                        length = ToLength(ReadBigEndian(data, ref position, 4));
                        break;
                    default:
                        throw new InvalidDataException($"Expected a string, found type code 0x{code:x2}");
                }
            }

            if (length > data.Length - position)
                throw new InvalidDataException("The data ends inside a string");
            var value = Utf8.GetString(data, position, length);
            position += length;
            return value;
        }

        private static byte ReadByte(byte[] data, ref int position)
        {
            if (position >= data.Length)
                throw new InvalidDataException("The data ends before the value it announces");
            return data[position++];
        }

        private static ulong ReadBigEndian(byte[] data, ref int position, int byteCount)
        {
            ulong value = 0;
            for (var i = 0; i < byteCount; i++)
                value = (value << 8) | ReadByte(data, ref position);
            return value;
        }

        private static int ToLength(ulong value)
        {
            if (value > int.MaxValue)
                throw new InvalidDataException($"Length {value} is too large");
            return (int)value;
        }

        private static void WriteBigEndian(Stream output, ulong value, int byteCount)
        {
            for (var shift = (byteCount - 1) * 8; shift >= 0; shift -= 8)
                output.WriteByte(unchecked((byte)(value >> shift)));
        }
    }
}
