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

namespace DotNetWorkQueue.Dashboard.Ui.Services
{
    /// <summary>
    /// In-memory registry of configured Dashboard API sources.
    /// Validates uniqueness of names and slugs at construction time
    /// and provides O(1) lookups by slug or name.
    /// </summary>
    public class SourceRegistry : ISourceRegistry
    {
        private readonly IReadOnlyList<DashboardApiSourceConfig> _sources;
        private readonly Dictionary<string, DashboardApiSourceConfig> _bySlug;
        private readonly Dictionary<string, DashboardApiSourceConfig> _byName;

        /// <summary>
        /// Creates a new source registry from the provided sources.
        /// </summary>
        /// <param name="sources">The list of API sources to register. Must contain at least one entry with unique names and slugs.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="sources"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="sources"/> is empty, or contains duplicate names or slugs.</exception>
        public SourceRegistry(IReadOnlyList<DashboardApiSourceConfig> sources)
        {
            ArgumentNullException.ThrowIfNull(sources);

            if (sources.Count == 0)
            {
                throw new ArgumentException("At least one API source must be configured.", nameof(sources));
            }

            _byName = new Dictionary<string, DashboardApiSourceConfig>(StringComparer.OrdinalIgnoreCase);
            _bySlug = new Dictionary<string, DashboardApiSourceConfig>(StringComparer.Ordinal);

            foreach (var source in sources)
            {
                if (!_byName.TryAdd(source.Name, source))
                {
                    throw new ArgumentException(
                        $"A duplicate API source name was found: '{source.Name}'. Each source must have a unique name.",
                        nameof(sources));
                }

                if (!_bySlug.TryAdd(source.Slug, source))
                {
                    throw new ArgumentException(
                        $"A duplicate slug '{source.Slug}' was produced by source '{source.Name}'. Each source must produce a unique slug.",
                        nameof(sources));
                }
            }

            _sources = sources;
        }

        /// <inheritdoc />
        public IReadOnlyList<DashboardApiSourceConfig> GetAll()
        {
            return _sources;
        }

        /// <inheritdoc />
        public DashboardApiSourceConfig? GetBySlug(string slug)
        {
            return _bySlug.GetValueOrDefault(slug);
        }

        /// <inheritdoc />
        public DashboardApiSourceConfig? GetByName(string name)
        {
            return _byName.GetValueOrDefault(name);
        }
    }
}
