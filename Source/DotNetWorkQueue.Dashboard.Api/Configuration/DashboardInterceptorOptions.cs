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

namespace DotNetWorkQueue.Dashboard.Api.Configuration
{
    /// <summary>
    /// JSON-bindable configuration for built-in message interceptors.
    /// </summary>
    public class DashboardInterceptorOptions
    {
        /// <summary>
        /// Gets or sets GZip compression interceptor options.
        /// Set to null (or omit from JSON) to disable.
        /// </summary>
        public GZipInterceptorOptions GZip { get; set; }

        /// <summary>
        /// Gets or sets TripleDES encryption interceptor options.
        /// Set to null (or omit from JSON) to disable.
        /// </summary>
        public TripleDesInterceptorOptions TripleDes { get; set; }
    }

    /// <summary>
    /// JSON-bindable options for the GZip message interceptor.
    /// </summary>
    public class GZipInterceptorOptions
    {
        /// <summary>
        /// Gets or sets whether GZip compression is enabled. Default is true.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the minimum message size in bytes before compression is applied. Default is 150.
        /// </summary>
        public int MinimumSize { get; set; } = 150;
    }

    /// <summary>
    /// JSON-bindable options for the TripleDES encryption interceptor.
    /// </summary>
    public class TripleDesInterceptorOptions
    {
        /// <summary>
        /// Gets or sets whether TripleDES encryption is enabled. Default is true.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the encryption key as a Base64-encoded string.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the initialization vector as a Base64-encoded string.
        /// </summary>
        public string IV { get; set; }
    }
}
