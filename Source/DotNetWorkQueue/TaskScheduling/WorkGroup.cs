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
using DotNetWorkQueue.Validation;

namespace DotNetWorkQueue.TaskScheduling
{
    /// <summary>
    /// Allows setting concurrency levels for <see cref="ITaskScheduler"/>
    /// </summary>
    /// <remarks>For hashing and <see cref="Equals"/> comparisons the <see cref="Name"/> is always used.</remarks>
    internal class WorkGroup : IWorkGroup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WorkGroup"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="concurrencyLevel">The concurrency level.</param>
        public WorkGroup(string name, int concurrencyLevel)
        {
            Guard.NotNullOrEmpty(() => name, name);
            Guard.IsValid(() => concurrencyLevel, concurrencyLevel, i => i > 0,
               "concurrencyLevel must be greater than 0");

            Name = name;
            ConcurrencyLevel = concurrencyLevel;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; }
        /// <summary>
        /// Gets the concurrency level.
        /// </summary>
        /// <value>
        /// The concurrency level.
        /// </value>
        public int ConcurrencyLevel { get; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String" /> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            // Check for null values and compare run-time types.
            if (obj == null || GetType() != obj.GetType())
                return false;

            var workGroup = (WorkGroup)obj;
            return Name == workGroup.Name;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}
