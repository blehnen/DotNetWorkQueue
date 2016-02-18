// ---------------------------------------------------------------------
//This file is part of DotNetWorkQueue
//Copyright © 2016 Brian Lehnen
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
using DotNetWorkQueue.Exceptions;

namespace DotNetWorkQueue.Transport.SQLite.Schema
{
    /// <summary>
    /// A collection of <seealso cref="Constraint"/>
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable", Justification = "Not supported by children")]
    public class ConstraintList : Dictionary<string, Constraint>
    {
        /// <summary>
        /// Adds the specified constraint
        /// </summary>
        /// <param name="constraint">The constraint.</param>
        public void Add(Constraint constraint)
        {
            if (!ContainsKey(constraint.Name))
            {
                Add(constraint.Name, constraint);
            }
            else
            {
                throw new DotNetWorkQueueException($"Duplicate constraint name {constraint.Name}");
            }
        }

        /// <summary>
        /// Removes the specified constraint
        /// </summary>
        /// <param name="constraint">The constraint.</param>
        public void Remove(Constraint constraint)
        {
            if (ContainsKey(constraint.Name))
            {
                Remove(constraint.Name);
            }
        }
    }
}
