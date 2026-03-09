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

namespace DotNetWorkQueue.Metrics.Net
{
    internal static class TagsHelper
    {
        private static readonly KeyValuePair<string, object>[] Empty = new KeyValuePair<string, object>[0];

        public static KeyValuePair<string, object>[] Convert(List<KeyValuePair<string, string>> tags)
        {
            if (tags == null || tags.Count == 0)
                return Empty;

            var result = new KeyValuePair<string, object>[tags.Count];
            for (var i = 0; i < tags.Count; i++)
            {
                result[i] = new KeyValuePair<string, object>(tags[i].Key, tags[i].Value);
            }
            return result;
        }
    }
}
