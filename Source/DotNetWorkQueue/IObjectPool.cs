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
using System;

namespace DotNetWorkQueue
{
    /// <summary>
    /// A basic pooling strategy for objects, based on the MSDN example: https://msdn.microsoft.com/en-us/library/ff458671(v=vs.110).aspx
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IObjectPool<T> : IDisposable where T : IPooledObject
    {
        /// <summary>
        /// A factory for creating new objects for the pool.
        /// </summary>
        Func<T> Factory { get; }

        /// <summary>
        /// Defines the maximum pool size
        /// </summary>
        int MaximumPoolSize { get; }

        /// <summary>
        /// Returns an object from the pool, or a new object if the pool is full.
        /// </summary>
        /// <returns>A monitored object from the pool.</returns>
        T GetObject();

        /// <summary>
        /// Returns the object to the pool, if the pool is not over the <seealso cref="MaximumPoolSize"/>
        /// </summary>
        /// <param name="value">The value.</param>
        void ReturnObject(T value);
    }
}
