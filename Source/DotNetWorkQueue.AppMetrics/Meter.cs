// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2015-2022 Brian Lehnen
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
namespace DotNetWorkQueue.AppMetrics
{
    /// <inheritdoc />
    internal class Meter : IMeter
    {
        private readonly App.Metrics.Meter.IMeter _meter;
        /// <summary>
        /// Initializes a new instance of the <see cref="Meter"/> class.
        /// </summary>
        /// <param name="meter">The meter.</param>
        public Meter(App.Metrics.Meter.IMeter meter)
        {
            _meter = meter;
        }

        /// <inheritdoc />
        public void Mark()
        {
            _meter.Mark();
        }

        /// <inheritdoc />
        public void Mark(string item)
        {
            _meter.Mark(item);
        }

        /// <inheritdoc />
        public void Mark(long count)
        {
            _meter.Mark(count);
        }

        /// <inheritdoc />
        public void Mark(string item, long count)
        {
            _meter.Mark(item, count);
        }
    }
}
