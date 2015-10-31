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

namespace DotNetWorkQueue.Metrics.Net
{
    /// <summary>
    /// A Histogram measures the distribution of values in a stream of data: e.g., the number of results returned by a search.
    /// </summary>
    internal class Histogram : IHistogram
    {
        private readonly global::Metrics.Histogram _histogram;
        /// <summary>
        /// Initializes a new instance of the <see cref="Histogram"/> class.
        /// </summary>
        /// <param name="histogram">The histogram.</param>
        public Histogram(global::Metrics.Histogram histogram)
        {
            _histogram = histogram;
        }
        /// <summary>
        /// Records a value.
        /// </summary>
        /// <param name="value">Value to be added to the histogram.</param>
        /// <param name="userValue">A custom user value that will be associated to the results.
        /// Useful for tracking (for example) for which id the max or min value was recorded.</param>
        public void Update(long value, string userValue = null)
        {
            _histogram.Update(value, userValue);
        }
    }
}
