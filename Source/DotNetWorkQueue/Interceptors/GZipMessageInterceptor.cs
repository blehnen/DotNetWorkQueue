// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015 Brian Lehnen
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

using System.IO;
using System.IO.Compression;
namespace DotNetWorkQueue.Interceptors
{
    /// <summary>
    /// Compresses and de-compress messages using the built in GZIP library
    /// </summary>
    public class GZipMessageInterceptor : IMessageInterceptor
    {
        private readonly GZipMessageInterceptorConfiguration _configuration;
        /// <summary>
        /// Initializes a new instance of the <see cref="GZipMessageInterceptor" /> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public GZipMessageInterceptor(GZipMessageInterceptorConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Runs the interceptor on the input and returns the output as a byte array. Used to serialize a message stream.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        public MessageInterceptorResult MessageToBytes(byte[] input)
        {
            Guard.NotNull(() => input, input);
            if (input.Length < _configuration.MiniumSize)
            {
                return new MessageInterceptorResult(input, false, GetType());
            }
            var outStream = SharedMemoryStream.StreamManager.GetStream("gzip-compress");
            using (var gZipStream = new GZipStream(outStream, CompressionMode.Compress, true))
            using (var memoryStream = new MemoryStream(input))
                memoryStream.CopyTo(gZipStream);
            return new MessageInterceptorResult(outStream.ToArray(), true, GetType());
        }

        /// <summary>
        /// Runs the interceptor on the input and returns the output as a byte array. Used to re-construct a message stream.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns></returns>
        public byte[] BytesToMessage(byte[] input)
        {
            Guard.NotNull(() => input, input);
            var memoryStream = new MemoryStream(input);
            using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress, true))
            using (var destination = SharedMemoryStream.StreamManager.GetStream("gzip - uncompressed - output"))
            {
                gZipStream.CopyTo(destination);
                return destination.ToArray();
            }
        }
    }

    /// <summary>
    /// Configuration class for <see cref="GZipMessageInterceptor"/>
    /// </summary>
    public class GZipMessageInterceptorConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GZipMessageInterceptorConfiguration"/> class.
        /// </summary>
        public GZipMessageInterceptorConfiguration()
        {
            MiniumSize = 150;
        }

        /// <summary>
        /// Gets or sets the minimum size of the input data as number of bytes
        /// </summary>
        /// <value>
        /// The minimum size of the input data, in bytes.
        /// </value>
        /// <remarks>Input data less than this size will not be compressed. Default is 150</remarks>
        public int MiniumSize { get; set; }
    }
}
