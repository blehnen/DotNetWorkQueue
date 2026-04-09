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

using System.Text.RegularExpressions;

namespace DotNetWorkQueue.Dashboard.Ui.Services
{
    /// <summary>
    /// Configuration for a single Dashboard API source endpoint.
    /// </summary>
    public class DashboardApiSourceConfig
    {
        /// <summary>
        /// The display name of this API source.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The base URL of the API endpoint.
        /// </summary>
        public string BaseUrl { get; set; } = string.Empty;

        /// <summary>
        /// An optional API key for authenticating with this source.
        /// </summary>
        public string? ApiKey { get; set; }

        /// <summary>
        /// A URL-safe slug derived from <see cref="Name"/>.
        /// Lowercase, non-alphanumeric characters replaced with hyphens,
        /// consecutive hyphens collapsed, leading/trailing hyphens trimmed.
        /// </summary>
        public string Slug => Slugify(Name);

        /// <summary>
        /// Converts a name into a URL-safe slug.
        /// </summary>
        /// <param name="name">The name to slugify.</param>
        /// <returns>A lowercase, hyphen-separated slug.</returns>
        private static string Slugify(string name)
        {
            var slug = name.Trim().ToLowerInvariant();
            slug = Regex.Replace(slug, @"[^a-z0-9\-]", "-");
            slug = Regex.Replace(slug, @"-{2,}", "-");
            slug = slug.Trim('-');
            return slug;
        }
    }
}
